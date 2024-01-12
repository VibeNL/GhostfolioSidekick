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
		private readonly MemoryCache memoryCache;
		private ILogger<GhostfolioAPI> logger;
		private readonly ModelToContractMapper modelToContractMapper;
		private readonly SymbolMapper mapper;
		private RestCall restCall;

		private List<string> SortorderDataSources { get; set; }

		public bool AllowAdminCalls { get; private set; } = true;

		public GhostfolioAPI(
			IApplicationSettings settings,
			MemoryCache memoryCache,
			ILogger<GhostfolioAPI> logger)
		{
			ArgumentNullException.ThrowIfNull(settings);

			ArgumentNullException.ThrowIfNull(memoryCache);

			this.settings = settings;
			this.memoryCache = memoryCache;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			restCall = new RestCall(memoryCache, logger, settings.GhostfolioUrl, settings.GhostfolioAccessToken);
			modelToContractMapper = new ModelToContractMapper(new CurrentPriceCalculator(this));
			mapper = new SymbolMapper(settings.ConfigurationInstance.Mappings ?? []);

			SortorderDataSources = [.. settings.ConfigurationInstance.Settings.DataProviderPreference.Split(',') ?? []];
		}

		public async Task<Model.Account?> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Short());
			if (content == null)
			{
				return null;
			}

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts!.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				return null;
			}

			content = await restCall.DoRestGet($"api/v1/order?accounts={rawAccount.Id}", CacheDuration.None());
			var activities = JsonConvert.DeserializeObject<ActivityList>(content!)?.Activities;

			if (activities == null)
			{
				return null;
			}

			return ContractToModelMapper.MapAccount(rawAccount, activities);
		}

		public async Task<Model.Platform?> GetPlatformByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/platform", CacheDuration.None());

			if (content == null)
			{
				return null;
			}

			var rawPlatforms = JsonConvert.DeserializeObject<Contract.Platform[]>(content);
			var rawPlatform = rawPlatforms?.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawPlatform == null)
			{
				return null;
			}

			return ContractToModelMapper.MapAccount(rawPlatform);
		}

		public async Task UpdateAccount(Model.Account account)
		{
			var existingAccount = await GetAccountByName(account.Name);

			if (existingAccount == null)
			{
				return;
			}

			var balance = GetBalance(account.Balance);

			await UpdateBalance(account, balance);

			var newActivities = account.Activities
				.Select(x => modelToContractMapper.ConvertToGhostfolioActivity(account, x))
				.Where(x => x != null && x.Type != Contract.ActivityType.IGNORE)
				.Select(Round!)
				.Where(x => x.Quantity != 0 || x.Fee != 0)
				.ToList();

			var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.Short());

			if (content == null)
			{
				return;
			}

			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content)?.Activities ?? [];

			var mergeOrders = MergeOrders(newActivities, existingActivities).OrderBy(x => x.Operation).OrderBy(x => x.Order1?.Date ?? x.Order2?.Date ?? DateTime.MaxValue).ToList();
			foreach (var mergeOrder in mergeOrders)
			{
				try
				{
					switch (mergeOrder.Operation)
					{
						case Operation.New:
							await WriteOrder(mergeOrder.Order1!);
							break;
						case Operation.Duplicate:
							// Nothing to do!
							break;
						case Operation.Updated:
							await DeleteOrder(mergeOrder.Order2!);
							await WriteOrder(mergeOrder.Order1!);
							break;
						case Operation.Removed:
							await DeleteOrder(mergeOrder.Order2!);
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

		public async Task<Money?> GetMarketPrice(Model.SymbolProfile asset, DateTime date)
		{
			if (!AllowAdminCalls)
			{
				return null;
			}

			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.None());

			if (content == null)
			{
				return null;
			}

			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content);

			var marketData = market?.MarketData?.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice, date);
		}

		public async Task<Model.SymbolProfile?> FindSymbolByIdentifier(
			string[] identifiers,
			Currency? expectedCurrency,
			AssetClass[]? expectedAssetClass,
			AssetSubClass[]? expectedAssetSubClass,
			bool checkExternalDataProviders,
			bool includeIndexes)
		{
			if (identifiers == null || !identifiers.Any())
			{
				return null;
			}

			var key = new CacheKey(identifiers, expectedAssetClass, expectedAssetSubClass);

			if (memoryCache.TryGetValue(key, out CacheValue? cacheValue))
			{
				return cacheValue!.Asset;
			}

			bool isCrypto = expectedAssetSubClass?.Contains(AssetSubClass.CRYPTOCURRENCY) ?? false;
			var allIdentifiers = identifiers
				.Union(identifiers.Select(x => mapper.MapSymbol(x)))
				.Union((IEnumerable<string>)(isCrypto ? identifiers.Select(CreateCryptoForYahoo) : []))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct();

			var foundAsset = await FindByMarketData(allIdentifiers);

			if (checkExternalDataProviders)
			{
				foundAsset ??= await FindByDataProvider(allIdentifiers, expectedCurrency, expectedAssetClass, expectedAssetSubClass, includeIndexes);
			}

			if (foundAsset != null)
			{
				AddToCache(key, foundAsset, memoryCache);
				await UpdateKnownIdentifiers(foundAsset, identifiers);
				return foundAsset;
			}

			AddToCache(key, null, memoryCache);
			logger.LogError($"Could not find any identifier [{string.Join(",", identifiers)}] as a symbol");
			return null;

			static void AddToCache(CacheKey key, Model.SymbolProfile? asset, IMemoryCache cache)
			{
				cache.Set(key, new CacheValue(asset), asset != null ? CacheDuration.Long() : CacheDuration.Short());
			}

			async Task<Model.SymbolProfile?> FindByMarketData(IEnumerable<string> allIdentifiers)
			{
				try
				{
					var r = (await GetMarketData(false)).Select(x => x.AssetProfile);

					foreach (var identifier in allIdentifiers)
					{
						var foundSymbol = r
							.Where(x => expectedAssetClass?.Contains(x.AssetClass) ?? true)
							.Where(x => expectedAssetSubClass?.Contains(x.AssetSubClass.GetValueOrDefault()) ?? true)
							.SingleOrDefault(x =>
							x.Symbol == identifier ||
							x.ISIN == identifier ||
							x.Identifiers.Contains(identifier));
						if (foundSymbol != null)
						{
							return foundSymbol;
						}
					}
				}
				catch (NotAuthorizedException)
				{
					// Ignore for now
				}

				return null;
			}

			async Task<Model.SymbolProfile?> FindByDataProvider(
				IEnumerable<string> ids,
				Currency? expectedCurrency,
				AssetClass[]? expectedAssetClass,
				AssetSubClass[]? expectedAssetSubClass,
				bool includeIndexes)
			{
				var identifiers = ids.ToList();
				var allAssets = new List<Model.SymbolProfile>();

				foreach (var identifier in identifiers)
				{
					for (var i = 0; i < 5; i++)
					{
						var content = await restCall.DoRestGet(
							$"api/v1/symbol/lookup?query={identifier.Trim()}&includeIndices={includeIndexes.ToString().ToLowerInvariant()}",
							CacheDuration.None());
						if (content == null)
						{
							continue;
						}

						var symbolProfileList = JsonConvert.DeserializeObject<SymbolProfileList>(content);
						var assets = symbolProfileList?.Items.Select(ContractToModelMapper.ParseSymbolProfile);

						if (assets?.Any() ?? false)
						{
							allAssets.AddRange(assets);
							break;
						}
					}
				}

				var filteredAsset = allAssets
					.Where(x => x != null)
					.Select(FixYahooCrypto)
					.Where(x => expectedAssetClass?.Contains(x.AssetClass) ?? true)
					.Where(x => expectedAssetSubClass?.Contains(x.AssetSubClass.GetValueOrDefault()) ?? true)
					.OrderBy(x => identifiers.Any(y => MatchId(x, y)) ? 0 : 1)
					.ThenByDescending(x => FussyMatch(identifiers, x))
					.ThenBy(x => string.Equals(x.Currency.Symbol, expectedCurrency?.Symbol, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
					.ThenBy(x => new[] { CurrencyHelper.EUR.Symbol, CurrencyHelper.USD.Symbol, CurrencyHelper.GBP.Symbol }.Contains(x.Currency.Symbol) ? 0 : 1) // prefer well known currencies
					.ThenBy(x =>
					{
						var index = SortorderDataSources.IndexOf(x.DataSource);
						if (index < 0)
						{
							index = int.MaxValue;
						}

						return index;
					}) // prefer Yahoo above Coingecko due to performance
					.ThenBy(x => x.Name?.Length ?? int.MaxValue)
					.FirstOrDefault();
				return filteredAsset;
			}

			bool MatchId(Model.SymbolProfile x, string id)
			{
				if (string.Equals(x.ISIN, id, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}

				if (string.Equals(x.Symbol, id, StringComparison.InvariantCultureIgnoreCase) ||
					(x.AssetSubClass == AssetSubClass.CRYPTOCURRENCY &&
					string.Equals(x.Symbol, id + "-USD", StringComparison.InvariantCultureIgnoreCase)) || // Add USD for Yahoo crypto
					(x.AssetSubClass == AssetSubClass.CRYPTOCURRENCY &&
					string.Equals(x.Symbol, id.Replace(" ", "-"), StringComparison.InvariantCultureIgnoreCase))) // Add dashes for CoinGecko
				{
					return true;
				}

				if (string.Equals(x.Name, id, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}

				return false;
			}

			Model.SymbolProfile FixYahooCrypto(Model.SymbolProfile x)
			{
				// Workaround for bug Ghostfolio
				if (x.AssetSubClass == AssetSubClass.CRYPTOCURRENCY && x.DataSource == "YAHOO" && x.Symbol.Length >= 6)
				{
					var t = x.Symbol;
					x.Symbol = string.Concat(t.AsSpan(0, t.Length - 3), "-", t.AsSpan(t.Length - 3, 3));
				}

				return x;
			}

			string CreateCryptoForYahoo(string x)
			{
				return x + "-USD";
			}

			int FussyMatch(List<string> identifiers, Model.SymbolProfile profile)
			{
				return identifiers.Max(x => Math.Max(FuzzySharp.Fuzz.Ratio(x, profile?.Name ?? string.Empty), FuzzySharp.Fuzz.Ratio(x, profile?.Symbol ?? string.Empty)));
			}
		}

		public async Task<Money?> GetConvertedPrice(Money? money, Currency targetCurrency, DateTime date)
		{
			if (money == null || money.Currency.Symbol == targetCurrency.Symbol || (money.Amount) == 0)
			{
				return money!;
			}

			var sourceCurrency = mapper.MapCurrency(money.Currency.Symbol);

			decimal rate = await GetConversionRate(CurrencyHelper.ParseCurrency(sourceCurrency), targetCurrency, date);
			return new Money(targetCurrency, rate * money.Amount, date);
		}

		public async Task<IEnumerable<Model.MarketDataList>> GetMarketData(bool filterBenchmarks = true)
		{
			if (!AllowAdminCalls)
			{
				return Enumerable.Empty<Model.MarketDataList>();
			}

			var content = await restCall.DoRestGet($"api/v1/admin/market-data/", CacheDuration.Short());

			if (content == null)
			{
				return [];
			}

			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content);

			var benchmarks = (await GetInfo()).BenchMarks ?? [];

			var filtered = filterBenchmarks ? market?.MarketData.Where(x => !benchmarks.Any(y => y.Symbol == x.Symbol)) : market?.MarketData;

			return filtered?.Select(x => GetMarketData(x.Symbol, x.DataSource).Result)?.ToList() ?? [];
		}

		public async Task DeleteSymbol(Model.SymbolProfile marketData)
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{marketData.DataSource}/{marketData.Symbol}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {marketData.Symbol}");
			}

			logger.LogInformation($"Deleted symbol {marketData.Symbol}");
		}

		public async Task CreateSymbol(Model.SymbolProfile asset)
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["symbol"] = asset.Symbol,
				["isin"] = asset.ISIN,
				["name"] = asset.Name,
				["comment"] = asset.Comment,
				["assetClass"] = asset.AssetClass.ToString(),
				["assetSubClass"] = asset.AssetSubClass?.ToString(),
				["currency"] = asset.Currency.Symbol,
				["datasource"] = asset.DataSource
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/admin/profile-data/{asset.DataSource}/{asset.Symbol}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {asset.Symbol}");
			}

			logger.LogInformation($"Created symbol {asset.Symbol}");

			// Set name and assetClass (BUG / Quirk Ghostfolio?)
			await UpdateSymbol(asset);

			ClearCache();
		}

		public async Task UpdateSymbol(Model.SymbolProfile asset)
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			JObject mappingObject = new();
			if (asset.Mappings.TrackInsight != null)
			{
				mappingObject.Add("TRACKINSIGHT", asset.Mappings.TrackInsight);
			}

			JObject scraperConfiguration = new();
			if (asset.ScraperConfiguration.IsValid)
			{
				scraperConfiguration.Add("url", asset.ScraperConfiguration.Url);
				scraperConfiguration.Add("selector", asset.ScraperConfiguration.Selector);
			}

			var o = new JObject
			{
				["name"] = asset.Name,
				["assetClass"] = asset.AssetClass.ToString(),
				["assetSubClass"] = asset.AssetSubClass?.ToString(),
				["comment"] = asset.Comment,
				["scraperConfiguration"] = scraperConfiguration,
				["symbolMapping"] = mappingObject
			};
			var res = o.ToString();

			try
			{
				var r = await restCall.DoRestPatch($"api/v1/admin/profile-data/{asset.DataSource}/{asset.Symbol}", res);
				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Update failed on symbol {asset.Symbol}");
				}
			}
			catch
			{
				throw new NotSupportedException($"Update failed on symbol {asset.Symbol}.");
			}

			logger.LogInformation($"Updated symbol {asset.Symbol}");

			ClearCache();
		}

		public async Task<Model.MarketDataList> GetMarketData(string symbol, string dataSource)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{dataSource}/{symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Contract.MarketDataList>(content!);

			return ContractToModelMapper.MapMarketDataList(market!);
		}

		public async Task<IEnumerable<Model.Activity>> GetAllActivities()
		{
			var content = await restCall.DoRestGet($"api/v1/order", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities;

			var assets = new ConcurrentDictionary<string, Model.SymbolProfile>();
			return existingActivities.Select(x => ContractToModelMapper.MapActivity(x, assets));
		}

		public async Task SetMarketPrice(Model.SymbolProfile assetProfile, Money money)
		{
			if (!AllowAdminCalls)
			{
				return;
			}

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

		public async Task CreatePlatform(Model.Platform platform)
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["name"] = platform.Name,
				["url"] = platform.Url
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/platform/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {platform.Name}");
			}

			logger.LogInformation($"Created platform {platform.Name}");
		}

		public async Task CreateAccount(Model.Account account)
		{
			var platform = await GetPlatformByName(account.Name);

			var o = new JObject
			{
				["name"] = account.Name,
				["currency"] = account.Balance.Currency.Symbol,
				["comment"] = account.Comment,
				["platformId"] = platform?.Id,
				["isExcluded"] = false,
				["balance"] = 0,
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/account/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {account.Name}");
			}

			logger.LogInformation($"Created account {account.Name}");
		}

		public async Task GatherAllMarktData()
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/admin/gather/max/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Gathering failed");
			}

			logger.LogInformation($"Gathering requested");
		}

		public async Task AddAndRemoveDummyCurrency()
		{
			if (!AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["value"] = "[\"USD\",\" \"]"
			};
			var res = o.ToString();

			var r = await restCall.DoRestPut($"api/v1/admin/settings/CURRENCIES", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Inserting dummy currency failed");
			}

			logger.LogInformation($"Inserted dummy currency");

			o = new JObject
			{
				["value"] = "[\"USD\"]"
			};
			res = o.ToString();

			r = await restCall.DoRestPut($"api/v1/admin/settings/CURRENCIES", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Removing dummy currency failed");
			}

			logger.LogInformation($"Removed dummy currency");
		}

		private async Task<GenericInfo> GetInfo()
		{
			var content = await restCall.DoRestGet($"api/v1/info/", CacheDuration.Short());
			return JsonConvert.DeserializeObject<GenericInfo>(content!)!;
		}

		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == Contract.ActivityType.IGNORE)
			{
				logger.LogDebug($"Skipping ignore transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			var url = $"api/v1/order";
			var r = await restCall.DoRestPost(url, await ConvertToBody(activity));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content?.Equals("{\"activities\":[]}") ?? true))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			logger.LogInformation($"Added transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
		}

		private async Task DeleteOrder(Contract.Activity order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Id} {order.SymbolProfile?.Symbol} {order.Date}");
		}

		private async Task UpdateBalance(Model.Account account, decimal balance)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Short());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content!);
			var rawAccount = rawAccounts?.Accounts?.SingleOrDefault(x => string.Equals(x.Id, account.Id, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				throw new NotSupportedException("Account not found");
			}

			if (Math.Round(rawAccount.Balance, 10) == Math.Round(balance, 10))
			{
				return;
			}

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

		private Task<string> ConvertToBody(Contract.Activity activity)
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
			else if (activity.Type == Ghostfolio.Contract.ActivityType.FEE)
			{
				o["symbol"] = "Fee";
			}
			else
			{
				o["symbol"] = activity.SymbolProfile?.Symbol;
			}
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return Task.FromResult(res);
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
				(fo.SymbolProfile?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == Contract.ActivityType.INTEREST || fo.Type == Contract.ActivityType.FEE) && // Interest & Fee create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
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

		private async Task<decimal> GetConversionRate(Currency? sourceCurrency, Currency targetCurrency, DateTime date)
		{
			if (sourceCurrency == null)
			{
				return 1;
			}

			var pairTo = CurrencyHelper.GetKnownPairOfCurrencies(targetCurrency);
			var pairFrom = CurrencyHelper.GetKnownPairOfCurrencies(sourceCurrency);

			try
			{
				foreach (var fromCurrency in pairFrom)
					foreach (var toCurrency in pairTo)
					{
						try
						{
							var content = await restCall.DoRestGet($"api/v1/exchange-rate/{fromCurrency.Symbol}-{toCurrency.Symbol}/{date:yyyy-MM-dd}", CacheDuration.Short(), true);
							if (content == null)
							{
								continue;
							}

							dynamic stuff = JsonConvert.DeserializeObject(content)!;
							var token = stuff!.marketPrice.ToString();

							var amount = pairFrom.CalculateRate(sourceCurrency, true) * pairTo.CalculateRate(targetCurrency, false);
							return amount * ((decimal)decimal.Parse(token));
						}
						catch
						{
						}
					}
			}
			catch
			{
				logger.LogWarning($"Exchange rate not found for {sourceCurrency}-{targetCurrency.Symbol} on {date}. Assuming rate of 1");
			}

			return 1;
		}

		private async Task UpdateKnownIdentifiers(Model.SymbolProfile foundAsset, params string[] identifiers)
		{
			if (!AllowAdminCalls)
			{
				return;
			}


			var change = false;
			foreach (var identifier in identifiers)
			{
				if (!foundAsset.Identifiers.Contains(identifier))
				{
					foundAsset.AddIdentifier(identifier);
					change = true;
				}
			}

			if (change)
			{
				var o = new JObject
				{
					["comment"] = foundAsset.Comment
				};
				var res = o.ToString();

				try
				{
					// Check if exists
					var md = await GetMarketData(foundAsset.Symbol, foundAsset.DataSource);

					if (string.IsNullOrWhiteSpace(md?.AssetProfile.Name) && md?.AssetProfile?.Currency?.Symbol == "-")
					{
						var newO = new JObject
						{
							["symbol"] = foundAsset.Symbol,
							["dataSource"] = foundAsset.DataSource
						};
						var newRes = newO.ToString();
						try
						{
							await restCall.DoRestPost($"api/v1/admin/profile-data/{foundAsset.DataSource}/{foundAsset.Symbol}", newRes);
							logger.LogInformation($"Created symbol {foundAsset.Symbol}");
						}
						catch
						{
							// Ignore for now
						}
					}

					var r = await restCall.DoRestPatch($"api/v1/admin/profile-data/{foundAsset.DataSource}/{foundAsset.Symbol}", res);
					logger.LogInformation($"Updated symbol {foundAsset.Symbol}, IDs {string.Join(",", identifiers)}");
				}
				catch
				{
					// Ignore for now
				}
			}
		}

		public void SetAllowAdmin(bool isallowed)
		{
			this.AllowAdminCalls = isallowed;
		}

		public void ClearCache()
		{
			this.memoryCache.Clear();
		}

		public async Task SetSymbolAsBenchmark(string symbol, string dataSource)
		{
			var currentBanchmarks = (await GetInfo()).BenchMarks!;
			if (currentBanchmarks.Any(x => x.Symbol == symbol))
			{
				return;
			}

			var o = new JObject
			{
				["datasource"] = dataSource,
				["symbol"] = symbol

			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/benchmark/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Updating symbol failed to mark as a benchmark {symbol}");
			}

			logger.LogInformation($"Updated symbol to be a benchmark {symbol}");
		}
	}
}
