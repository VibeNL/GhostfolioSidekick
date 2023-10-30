using GhostfolioSidekick.Ghostfolio.API.Mapper;
using GhostfolioSidekick.Ghostfolio.Contract;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public partial class GhostfolioAPI : IGhostfolioAPI
	{
		private readonly IApplicationSettings settings;
		private readonly IMemoryCache memoryCache;
		private ILogger<GhostfolioAPI> logger;
		private readonly ModelToContractMapper modelToContractMapper;
		private readonly SymbolMapper mapper;
		private RestCall restCall;

		public GhostfolioAPI(
			IApplicationSettings settings,
			IMemoryCache memoryCache,
			ILogger<GhostfolioAPI> logger)
		{
			if (settings is null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			if (memoryCache is null)
			{
				throw new ArgumentNullException(nameof(memoryCache));
			}

			this.settings = settings;
			this.memoryCache = memoryCache;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			restCall = new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken);
			modelToContractMapper = new ModelToContractMapper(new CurrentPriceCalculator(this));
			this.mapper = new SymbolMapper(settings.ConfigurationInstance.Mappings);
		}

		public async Task<Model.Account?> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.None());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				return null;
			}

			content = await restCall.DoRestGet($"api/v1/order?accounts={rawAccount.Id}", CacheDuration.None());
			var activities = JsonConvert.DeserializeObject<ActivityList>(content).Activities;

			return ContractToModelMapper.MapAccount(rawAccount, activities);
		}

		public async Task UpdateAccount(Model.Account account)
		{
			var existingAccount = await GetAccountByName(account.Name);

			var balance = GetBalance(account.Balance);

			await UpdateBalance(account, balance);

			var newActivities = account.Activities
				.Select(x => modelToContractMapper.ConvertToGhostfolioActivity(account, x))
				.Where(x => x != null)
				.Where(x => x.Type != Contract.ActivityType.IGNORE)
				.ToList();
			newActivities = newActivities.Select(Round).ToList();

			var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content).Activities;

			var mergeOrders = MergeOrders(newActivities, existingActivities).OrderBy(x => x.Operation).ToList();
			foreach (var mergeOrder in mergeOrders)
			{
				try
				{
					switch (mergeOrder.Operation)
					{
						case Operation.New:
							await WriteOrder(mergeOrder.Order1);
							break;
						case Operation.Duplicate:
							// Nothing to do!
							break;
						case Operation.Updated:
							await DeleteOrder(mergeOrder.Order2);
							await WriteOrder(mergeOrder.Order1);
							break;
						case Operation.Removed:
							await DeleteOrder(mergeOrder.Order2);
							break;
						default:
							throw new NotSupportedException();
					}
				}
				catch (Exception ex)
				{
					logger.LogError($"Transaction failed to write {ex}, skipping");
				}
			}
		}

		public async Task<Money?> GetMarketPrice(Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content);

			var marketData = market.MarketData.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice, date);
		}

		public async Task<Asset?> FindSymbolByIdentifier(
			string? identifier,
			Currency? expectedCurrency,
			AssetClass?[] expectedAssetClass,
			AssetSubClass?[] expectedAssetSubClass)
		{
			if (identifier == null)
			{
				return null;
			}

			if (memoryCache.TryGetValue(identifier, out Asset? asset))
			{
				return asset;
			}

			var mappedIdentifier = mapper.MapSymbol(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={mappedIdentifier.Trim()}", CacheDuration.Long());
			var symbolProfileList = JsonConvert.DeserializeObject<SymbolProfileList>(content);

			var assets = symbolProfileList.Items.Select(x => ContractToModelMapper.ParseSymbolProfile(x));

			var filteredAsset = assets
				.OrderBy(x => x.ISIN == identifier ? 0 : 1)
				.ThenBy(x => x.Symbol == identifier ? 0 : 1)
				.ThenBy(x => x.Name == identifier ? 0 : 1)
				.ThenBy(x => (expectedAssetClass?.Contains(x.AssetClass.GetValueOrDefault()) ?? false) ? 0 : 1)
				.ThenBy(x => (expectedAssetSubClass?.Contains(x.AssetSubClass.GetValueOrDefault()) ?? false) ? 0 : 1)
				.ThenBy(x => x.Currency.Symbol == expectedCurrency?.Symbol ? 0 : 1)
				.ThenBy(x => x.Name.Length)
				.FirstOrDefault();
			AddToCache(identifier, filteredAsset, memoryCache);
			return LogIfEmpty(filteredAsset, mappedIdentifier);

			static void AddToCache(string identifier, Asset? asset, IMemoryCache cache)
			{
				cache.Set(identifier, asset, CacheDuration.Long());
			}
		}

		public async Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date)
		{
			if (money == null || money.Currency.Symbol == targetCurrency.Symbol || (money.Amount) == 0)
			{
				return money;
			}

			var sourceCurrency = mapper.MapCurrency(money.Currency.Symbol);

			decimal rate = 1;
			try
			{
				var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency.Symbol}/{date:yyyy-MM-dd}", CacheDuration.Short(), true);

				if (content != null)
				{
					dynamic stuff = JsonConvert.DeserializeObject(content);
					var token = stuff.marketPrice.ToString();
					rate = (decimal)decimal.Parse(token);
				}
			}
			catch
			{
				logger.LogWarning($"Exchange rate not found for {sourceCurrency}-{targetCurrency.Symbol} on {date}. Assuming rate of 1");
			}

			if (rate == 1)
			{
				return money;
			}

			return new Money(targetCurrency, rate * money.Amount, date);
		}

		public async Task<IEnumerable<Model.MarketDataList>> GetMarketData()
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content);

			var benchmarks = (await GetInfo()).BenchMarks;

			var filtered = market.MarketData.Where(x => !benchmarks.Any(y => y.Symbol == x.Symbol));

			return filtered.Select(x => GetMarketData(x.Symbol, x.DataSource).Result).ToList();
		}

		public async Task DeleteSymbol(Model.SymbolProfile marketData)
		{
			var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{marketData.DataSource}/{marketData.Symbol}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {marketData.Symbol}");
			}

			logger.LogInformation($"Deleted symbol {marketData.Symbol}");
		}

		public async Task CreateManualSymbol(Asset asset)
		{
			var o = new JObject
			{
				["symbol"] = asset.Symbol,
				["isin"] = asset.ISIN,
				["name"] = asset.Name,
				["assetClass"] = asset.AssetClass?.ToString(),
				["assetSubClass"] = asset.AssetSubClass.ToString(),
				["currency"] = asset.Currency.Symbol,
				["datasource"] = asset.DataSource
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/admin/profile-data/{asset.DataSource}/{asset.Symbol}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {asset.Symbol}");
			}

			// Set name and assetClass (BUG / Quirk Ghostfolio?)
			o = new JObject
			{
				["name"] = asset.Name,
				["assetClass"] = asset.AssetClass.ToString(),
				["assetSubClass"] = asset.AssetSubClass.ToString(),
				["comment"] = string.Empty,
				["comment"] = string.Empty,
				["scraperConfiguration"] = new JObject(),
				["symbolMapping"] = new JObject()
			};
			res = o.ToString();

			try
			{
				r = await restCall.DoPatch($"api/v1/admin/profile-data/{asset.DataSource}/{asset.Symbol}", res);
				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Creation failed on update {asset.Symbol}");
				}
			}
			catch (Exception ex)
			{
				throw new NotSupportedException($"Creation failed on update {asset.Symbol}.");
			}

			logger.LogInformation($"Created symbol {asset.Symbol}");
		}

		public async Task<Model.MarketDataList> GetMarketData(string symbol, string dataSource)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{dataSource}/{symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content);

			return ContractToModelMapper.MapMarketDataList(market);
		}

		public async Task UpdateMarketData(Model.SymbolProfile marketData)
		{
			var o = new JObject();
			JObject mappingObject = new JObject();
			if (marketData.Mappings.TrackInsight != null)
			{
				mappingObject.Add("TRACKINSIGHT", marketData.Mappings.TrackInsight);
			}

			o["symbolMapping"] = mappingObject;
			var res = o.ToString();

			var r = await restCall.DoPatch($"api/v1/admin/profile-data/{marketData.DataSource}/{marketData.Symbol}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {marketData.Symbol}");
			}

			logger.LogInformation($"Deleted symbol {marketData.Symbol}");

		}

		public async Task<IEnumerable<Model.Activity>> GetAllActivities()
		{
			var content = await restCall.DoRestGet($"api/v1/order", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content).Activities;

			var assets = new ConcurrentDictionary<string, Asset>();
			return existingActivities.Select(x => ContractToModelMapper.MapActivity(x, assets));
		}

		public async Task SetMarketPrice(Model.SymbolProfile assetProfile, Money money)
		{
			var o = new JObject();
			o["marketPrice"] = money.Amount;

			var res = o.ToString();

			var r = await restCall.DoRestPut($"api/v1/admin/market-data/{assetProfile.DataSource}/{assetProfile.Symbol}/{money.TimeOfRecord:yyyy-MM-dd}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"SetMarketPrice failed {assetProfile.Symbol} {money.TimeOfRecord}");
			}

			logger.LogInformation($"SetMarketPrice symbol {assetProfile.Symbol} {money.TimeOfRecord} @ {money.Amount}");
		}

		private async Task<GenericInfo> GetInfo()
		{
			var content = await restCall.DoRestGet($"api/v1/info/", CacheDuration.Short());
			return JsonConvert.DeserializeObject<GenericInfo>(content);
		}

		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.SymbolProfile.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == Ghostfolio.Contract.ActivityType.IGNORE)
			{
				logger.LogDebug($"Skipping ignore transaction {activity.Date} {activity.SymbolProfile.Symbol} {activity.Quantity} {activity.Type}");
			}

			var url = $"api/v1/order";
			var r = await restCall.DoRestPost(url, await ConvertToBody(activity));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content.Equals("{\"activities\":[]}")))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {activity.Date} {activity.SymbolProfile.Symbol} {activity.Quantity} {activity.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {activity.Date} {activity.SymbolProfile.Symbol} {activity.Quantity} {activity.Type}");
			}

			logger.LogInformation($"Added transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
		}

		private async Task DeleteOrder(Contract.Activity? order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Id} {order.SymbolProfile.Symbol} {order.Date}");
		}

		private async Task UpdateBalance(Model.Account account, decimal balance)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Id, account.Id, StringComparison.InvariantCultureIgnoreCase));

			rawAccount.Balance = balance;

			var o = new JObject();
			o["balance"] = balance;
			o["comment"] = rawAccount.Comment;
			o["currency"] = rawAccount.Currency;
			o["id"] = rawAccount.Id;
			o["isExcluded"] = rawAccount.IsExcluded;
			o["name"] = rawAccount.Name;
			o["platformId"] = rawAccount.PlatformId;
			var res = o.ToString();

			await restCall.DoRestPut($"api/v1/account/{account.Id}", res);
		}

		private async Task<string> ConvertToBody(Contract.Activity activity)
		{
			var o = new JObject();
			o["accountId"] = activity.AccountId;
			o["comment"] = activity.Comment;
			o["currency"] = activity.Currency;
			o["dataSource"] = activity.SymbolProfile?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;

			if (activity.Type == Ghostfolio.Contract.ActivityType.INTEREST)
			{
				o["symbol"] = "Interest";
			}
			else
			{
				o["symbol"] = activity.SymbolProfile?.Symbol;
			}
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return res;
		}

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Contract.Activity> ordersFromFiles, IEnumerable<Contract.Activity> existingOrders)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";

			var existingOrdersWithMatchFlag = existingOrders.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			return ordersFromFiles.GroupJoin(existingOrdersWithMatchFlag,
				fo => fo.ReferenceCode,
				eo =>
				{
					if (string.IsNullOrWhiteSpace(eo.Activity.Comment))
					{
						return Guid.NewGuid().ToString();
					}

					var match = Regex.Match(eo.Activity.Comment, pattern);
					var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
					return key;
				},
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (AreEquals(fo, other.Activity))
						{
							return new MergeOrder(Operation.Duplicate, fo);
						}

						return new MergeOrder(Operation.Updated, fo, other.Activity);
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, fo);
					}
					else
					{
						throw new NotSupportedException();
					}
				}).Union(existingOrdersWithMatchFlag.Where(x => !x.IsMatched).Select(x => new MergeOrder(Operation.Removed, null, x.Activity)));
		}

		private bool AreEquals(Contract.Activity fo, Contract.Activity eo)
		{
			return
				(fo.SymbolProfile?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == Ghostfolio.Contract.ActivityType.INTEREST) && // Interest create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private Asset? LogIfEmpty(Asset? asset, string identifier)
		{
			if (asset == null)
			{
				logger.LogError($"Could not find {identifier} as a symbol");
			}

			return asset;
		}

		private Contract.Activity Round(Contract.Activity activity)
		{
			decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			};

			activity.Fee = Round(activity.Fee);
			activity.Quantity = Round(activity.Quantity);
			activity.UnitPrice = Round(activity.UnitPrice);

			return activity;
		}

		private decimal GetBalance(Balance balance)
		{
			return balance.Current(new CurrentPriceCalculator(this)).Amount;
		}
	}
}
