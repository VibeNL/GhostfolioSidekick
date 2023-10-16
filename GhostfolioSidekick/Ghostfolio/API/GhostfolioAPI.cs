using GhostfolioSidekick.Ghostfolio.API.Contract;
using GhostfolioSidekick.Ghostfolio.API.Mapper;
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
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			restCall = new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken);
			modelToContractMapper = new ModelToContractMapper(new CurrentPriceCalculator(this));
			this.mapper = new SymbolMapper(settings.ConfigurationInstance.Mappings);
		}

		public async Task<Model.Account?> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				return null;
			}

			content = await restCall.DoRestGet($"api/v1/order?accounts={rawAccount.Id}", CacheDuration.None());
			var rawOrders = JsonConvert.DeserializeObject<RawActivityList>(content).Activities;

			var assets = new ConcurrentDictionary<string, Model.Asset>();
			return ContractToModelMapper.MapActivity(rawAccount, rawOrders, assets);
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
			//newActivities = DateTimeCollisionFixer.Merge(newActivities).ToList();
			newActivities = newActivities.Select(Round).ToList();

			var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<RawActivityList>(content).Activities;

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

		public async Task<Money?> GetMarketPrice(Model.Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);

			var marketData = market.MarketData.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice, date);
		}

		public async Task<Model.Asset?> FindSymbolByISIN(string? identifier, Func<IEnumerable<Model.Asset>, Model.Asset?> selector)
		{
			identifier = mapper.MapSymbol(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={identifier.Trim()}", CacheDuration.Long());
			var rawAssetList = JsonConvert.DeserializeObject<AssetList>(content);

			var assets = rawAssetList.Items.Select(x => ContractToModelMapper.ParseSymbolProfile(x));

			if (selector == null)
			{
				return LogIfEmpty(assets.FirstOrDefault(), identifier);
			}

			return LogIfEmpty(selector(assets), identifier);
		}

		public async Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date)
		{
			if (money == null || money.Currency.Symbol == targetCurrency.Symbol || (money.Amount) == 0)
			{
				return money;
			}

			var sourceCurrency = mapper.MapCurrency(money.Currency.Symbol);

			var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency.Symbol}/{date:yyyy-MM-dd}", CacheDuration.Short());

			dynamic stuff = JsonConvert.DeserializeObject(content);
			var token = stuff.marketPrice.ToString();
			var rate = (decimal)decimal.Parse(token);

			if (rate == 1)
			{
				return money;
			}

			return new Money(targetCurrency, rate * money.Amount, date);
		}

		public async Task<IEnumerable<Model.MarketDataInfo>> GetMarketDataInfo()
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<MarketDataInfoList>(content);

			var benchmarks = (await GetInfo()).BenchMarks;

			return market.MarketData.Where(x => !benchmarks.Any(y => y.Symbol == x.Symbol)).Select(x => ContractToModelMapper.MapMarketDataInfo(x));
		}

		public async Task DeleteMarketData(Model.MarketDataInfo marketData)
		{
			// https://ghostfolio.vincentberkien.nl/api/v1/admin/profile-data/MANUAL/385c8b0f-cfba-4c9c-a6e9-468a68536d0f
			var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{marketData.DataSource}/{marketData.Symbol}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {marketData.Symbol}");
			}

			logger.LogInformation($"Deleted symbol {marketData.Symbol}");
		}

		public async Task<Model.MarketData> GetMarketData(Model.MarketDataInfo marketDataInfo)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{marketDataInfo.DataSource}/{marketDataInfo.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);

			return ContractToModelMapper.MapMarketData(market);
		}

		public async Task UpdateMarketData(Model.MarketData marketData)
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

		private async Task<GenericInfo> GetInfo()
		{
			var content = await restCall.DoRestGet($"api/v1/info/", CacheDuration.Short());
			return JsonConvert.DeserializeObject<GenericInfo>(content);
		}

		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == Contract.ActivityType.IGNORE)
			{
				logger.LogDebug($"Skipping ignore transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			var url = $"api/v1/order";
			var r = await restCall.DoRestPost(url, await ConvertToBody(activity));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content.Equals("{\"activities\":[]}")))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			logger.LogInformation($"Added transaction {activity.Date} {activity.Asset?.Symbol} {activity.Quantity} {activity.Type}");
		}

		private async Task DeleteOrder(RawActivity? order)
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
			o["dataSource"] = activity.Asset?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;

			if (activity.Type == Contract.ActivityType.INTEREST)
			{
				o["symbol"] = "Interest";
			}
			else
			{
				o["symbol"] = activity.Asset?.Symbol;
			}
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return res;
		}

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Contract.Activity> ordersFromFiles, IEnumerable<RawActivity> existingOrders)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";

			var existingOrdersWithMatchFlag = existingOrders.Select(x => new MatchRawActivity { Activity = x, IsMatched = false }).ToList();
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

		private bool AreEquals(Contract.Activity fo, RawActivity eo)
		{
			return
				(fo.Asset?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == Contract.ActivityType.INTEREST) && // Interest create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private Model.Asset? LogIfEmpty(Model.Asset? asset, string identifier)
		{
			if (asset == null)
			{
				logger.LogError($"Could not find {identifier} as a symbol");
			}

			return asset;
		}
	}
}
