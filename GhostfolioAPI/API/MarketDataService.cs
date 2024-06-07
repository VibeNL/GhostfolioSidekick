using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SymbolProfile = GhostfolioSidekick.Model.Symbols.SymbolProfile;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class MarketDataService : IMarketDataService
	{
		private readonly IApplicationSettings settings;
		private readonly MemoryCache memoryCache;
		private readonly ILogger<MarketDataService> logger;
		private readonly RestCall restCall;
		private readonly SymbolMapper symbolMapper;

		private List<string> SortorderDataSources { get; set; }

		public MarketDataService(
				IApplicationSettings settings,
				MemoryCache memoryCache,
				RestCall restCall,
				ILogger<MarketDataService> logger)
		{
			ArgumentNullException.ThrowIfNull(settings);
			ArgumentNullException.ThrowIfNull(memoryCache);
			this.settings = settings;
			this.memoryCache = memoryCache;
			this.logger = logger;
			SortorderDataSources = [.. settings.ConfigurationInstance.Settings.DataProviderPreference.Split(',').Select(x => x.ToUpperInvariant())];

			symbolMapper = new SymbolMapper(settings.ConfigurationInstance.Mappings ?? []);
			this.restCall = restCall;
		}

		public async Task<SymbolProfile?> FindSymbolByIdentifier(
			string[] identifiers,
			Currency? expectedCurrency,
			AssetClass[]? allowedAssetClass,
			AssetSubClass[]? allowedAssetSubClass,
			bool checkExternalDataProviders,
			bool includeIndexes)
		{
			if (identifiers == null || identifiers.Length == 0)
			{
				return null;
			}

			var key = new CacheKey(identifiers, allowedAssetClass, allowedAssetSubClass);

			if (memoryCache.TryGetValue(key, out CacheValue? cacheValue))
			{
				return cacheValue!.Asset;
			}

			bool isCrypto = allowedAssetSubClass?.Contains(AssetSubClass.CryptoCurrency) ?? false;

			var allIdentifiers = identifiers
				.Union(identifiers.Select(x => symbolMapper.MapSymbol(x)))
				.Union(isCrypto ? identifiers.Select(CryptoMapper.Instance.GetFullname) : [])
				.Union(isCrypto ? identifiers.Select(CreateCryptoForYahoo) : [])
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct();

			var foundAsset = await FindByMarketData(allIdentifiers);

			if (checkExternalDataProviders)
			{
				foundAsset ??= await FindByDataProvider(allIdentifiers, expectedCurrency, allowedAssetClass, allowedAssetSubClass, includeIndexes);
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

			static void AddToCache(CacheKey key, SymbolProfile? asset, IMemoryCache cache)
			{
				cache.Set(key, new CacheValue(asset), asset != null ? CacheDuration.Long() : CacheDuration.Short());
			}

			async Task<SymbolProfile?> FindByMarketData(IEnumerable<string> allIdentifiers)
			{
				try
				{
					var r = await GetAllSymbolProfiles();

					foreach (var identifier in allIdentifiers)
					{
						var foundSymbol = r
							.Where(x => allowedAssetClass?.Contains(x.AssetClass) ?? true)
							.Where(x => allowedAssetSubClass?.Contains(x.AssetSubClass.GetValueOrDefault()) ?? true)
							.SingleOrDefault(x =>
								x.Symbol == identifier ||
								x.ISIN == identifier ||
								x.Identifiers.Any(x => x.Equals(identifier, StringComparison.InvariantCultureIgnoreCase)));
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

			async Task<SymbolProfile?> FindByDataProvider(
				IEnumerable<string> ids,
				Currency? expectedCurrency,
				AssetClass[]? expectedAssetClass,
				AssetSubClass[]? expectedAssetSubClass,
				bool includeIndexes)
			{
				var identifiers = ids.ToList();
				var allAssets = new List<SymbolProfile>();

				foreach (var identifier in identifiers)
				{
					for (var i = 0; i < 5; i++)
					{
						var content = await restCall.DoRestGet(
							$"api/v1/symbol/lookup?query={identifier.Trim()}&includeIndices={includeIndexes.ToString().ToLowerInvariant()}");
						if (content == null)
						{
							continue;
						}

						var symbolProfileList = JsonConvert.DeserializeObject<SymbolProfileList>(content);
						var assets = symbolProfileList?.Items.Select(ContractToModelMapper.MapSymbolProfile);

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
					.OrderBy(x => identifiers.Exists(y => MatchId(x, y)) ? 0 : 1)
					.ThenByDescending(x => FussyMatch(identifiers, x))
					.ThenBy(x => x.AssetSubClass == AssetSubClass.CryptoCurrency && x.Name.Contains("[OLD]") ? 1 : 0)
					.ThenBy(x => string.Equals(x.Currency.Symbol, expectedCurrency?.Symbol, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
					.ThenBy(x => new[] { Currency.EUR.Symbol, Currency.USD.Symbol, Currency.GBP.Symbol, Currency.GBp.Symbol }.Contains(x.Currency.Symbol) ? 0 : 1) // prefer well known currencies
					.ThenBy(x =>
					{
						var index = SortorderDataSources.IndexOf(x.DataSource.ToString().ToUpperInvariant());
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

			bool MatchId(SymbolProfile x, string id)
			{
				if (string.Equals(x.ISIN, id, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}

				if (string.Equals(x.Symbol, id, StringComparison.InvariantCultureIgnoreCase) ||
					(x.AssetSubClass == AssetSubClass.CryptoCurrency &&
					string.Equals(x.Symbol, id + "-USD", StringComparison.InvariantCultureIgnoreCase)) || // Add USD for Yahoo crypto
					(x.AssetSubClass == AssetSubClass.CryptoCurrency &&
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

			SymbolProfile FixYahooCrypto(SymbolProfile x)
			{
				// Workaround for bug Ghostfolio
				if (x.AssetSubClass == AssetSubClass.CryptoCurrency && Model.Symbols.Datasource.YAHOO.ToString().Equals(x.DataSource, StringComparison.InvariantCultureIgnoreCase) && x.Symbol.Length >= 6)
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

			int FussyMatch(List<string> identifiers, SymbolProfile profile)
			{
				return identifiers.Max(x => Math.Max(FuzzySharp.Fuzz.Ratio(x, profile?.Name ?? string.Empty), FuzzySharp.Fuzz.Ratio(x, profile?.Symbol ?? string.Empty)));
			}
		}

		public async Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles()
		{
			if (!settings.AllowAdminCalls)
			{
				return Enumerable.Empty<SymbolProfile>();
			}

			var key = $"{nameof(MarketDataService)}{nameof(GetAllSymbolProfiles)}";
			if (memoryCache.TryGetValue(key, out IEnumerable<SymbolProfile>? cacheValue))
			{
				return cacheValue!;
			}

			var content = await restCall.DoRestGet($"api/v1/admin/market-data/");

			if (content == null)
			{
				return [];
			}

			var market = JsonConvert.DeserializeObject<MarketDataList>(content);

			var profiles = new List<SymbolProfile>();
			foreach (var f in market?.MarketData
				.Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && !string.IsNullOrWhiteSpace(x.DataSource))
				.ToList() ?? [])
			{
				content = await restCall.DoRestGet($"api/v1/admin/market-data/{f.DataSource}/{f.Symbol}");
				var data = JsonConvert.DeserializeObject<MarketDataListNoMarketData>(content!);
				profiles.Add(ContractToModelMapper.MapSymbolProfile(data!.AssetProfile));
			}

			memoryCache.Set(key, profiles);
			return profiles;
		}

		public async Task<MarketDataProfile> GetMarketData(string symbol, string dataSource)
		{
			if (!settings.AllowAdminCalls)
			{
				return new MarketDataProfile()
				{
					AssetProfile = new SymbolProfile(symbol, symbol, Currency.USD, dataSource, AssetClass.Undefined, null, [], []),
					MarketData = []
				};
			}

			var key = $"{nameof(MarketDataService)}{nameof(GetMarketData)}{symbol}{dataSource}";
			if (memoryCache.TryGetValue(key, out MarketDataProfile? cacheValue))
			{
				return cacheValue!;
			}

			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{dataSource}/{symbol}");
			var market = JsonConvert.DeserializeObject<MarketDataList>(content!);

			var r = ContractToModelMapper.MapMarketDataList(market!);
			memoryCache.Set(key, r);
			return r;
		}

		private async Task UpdateKnownIdentifiers(SymbolProfile foundAsset, params string[] identifiers)
		{
			if (!settings.AllowAdminCalls)
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
					var md = await GetMarketData(foundAsset.Symbol, foundAsset.DataSource.ToString().ToUpperInvariant());

					if (string.IsNullOrWhiteSpace(md?.AssetProfile.Name) && md?.AssetProfile?.Currency?.Symbol == "-")
					{
						var newO = new JObject
						{
							["symbol"] = foundAsset.Symbol,
							["dataSource"] = foundAsset.DataSource.ToString().ToUpperInvariant(),
						};
						var newRes = newO.ToString();
						try
						{
							await restCall.DoRestPost($"api/v1/admin/profile-data/{foundAsset.DataSource}/{foundAsset.Symbol}", newRes);
							logger.LogDebug($"Created symbol {foundAsset.Symbol}");
						}
						catch
						{
							// Ignore for now
						}
					}

					try
					{
						await restCall.DoRestPatch($"api/v1/admin/profile-data/{foundAsset.DataSource}/{foundAsset.Symbol}", res);
					}
					catch
					{
						// For some reason, the '-' is sometimes lost in translation
						await restCall.DoRestPatch($"api/v1/admin/profile-data/{foundAsset.DataSource}/{foundAsset.Symbol.Replace("-", "")}", res);
					}

					logger.LogDebug($"Updated symbol {foundAsset.Symbol}, IDs {string.Join(",", identifiers)}");
				}
				catch
				{
					// Ignore for now
				}
			}
		}

		public async Task<GenericInfo> GetInfo()
		{
			var content = await restCall.DoRestGet($"api/v1/info/");
			return JsonConvert.DeserializeObject<GenericInfo>(content!)!;
		}

		public async Task CreateSymbol(SymbolProfile symbolProfile)
		{
			if (!settings.AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["symbol"] = symbolProfile.Symbol,
				["isin"] = symbolProfile.ISIN,
				["name"] = symbolProfile.Name,
				["comment"] = symbolProfile.Comment,
				["assetClass"] = symbolProfile.AssetClass.ToString(),
				["assetSubClass"] = symbolProfile.AssetSubClass?.ToString(),
				["currency"] = symbolProfile.Currency.Symbol,
				["datasource"] = symbolProfile.DataSource.ToString(),
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {symbolProfile.Symbol}");
			}

			logger.LogDebug($"Created symbol {symbolProfile.Symbol}");

			// Set name and assetClass (BUG / Quirk Ghostfolio?)
			await UpdateSymbol(symbolProfile);

			ClearCache();
		}

		public async Task UpdateSymbol(SymbolProfile symbolProfile)
		{
			if (!settings.AllowAdminCalls)
			{
				return;
			}

			JObject mappingObject = [];
			if (symbolProfile.Mappings.TrackInsight != null)
			{
				mappingObject.Add("TRACKINSIGHT", symbolProfile.Mappings.TrackInsight);
			}

			JObject scraperConfiguration = [];
			if (symbolProfile.ScraperConfiguration.IsValid)
			{
				scraperConfiguration.Add("url", symbolProfile.ScraperConfiguration.Url);
				scraperConfiguration.Add("selector", symbolProfile.ScraperConfiguration.Selector);

				if (!string.IsNullOrWhiteSpace(symbolProfile.ScraperConfiguration.Locale))
				{
					scraperConfiguration.Add("locale", symbolProfile.ScraperConfiguration.Locale);
				}
			}

			JArray countries = [];
			foreach (var country in symbolProfile.Countries)
			{
				countries.Add(new JObject
				{
					["code"] = country.Code,
					["weight"] = country.Weight.ToString(),
					["continent"] = country.Continent,
					["name"] = country.Name,
				});
			}

			JArray sectors = [];
			foreach (var sector in symbolProfile.Sectors)
			{
				sectors.Add(new JObject
				{
					["weight"] = sector.Weight.ToString(),
					["name"] = sector.Name,
				});
			}

			var o = new JObject
			{
				["name"] = symbolProfile.Name,
				["assetClass"] = EnumMapper.ConvertAssetClassToString(symbolProfile.AssetClass),
				["assetSubClass"] = EnumMapper.ConvertAssetSubClassToString(symbolProfile.AssetSubClass),
				["comment"] = symbolProfile.Comment ?? string.Empty,
				["scraperConfiguration"] = scraperConfiguration,
				["symbolMapping"] = mappingObject,
				["countries"] = countries,
				["sectors"] = sectors
			};
			var res = o.ToString();

			try
			{
				var r = await restCall.DoRestPatch($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}", res);
				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Update failed on symbol {symbolProfile.Symbol}");
				}
			}
			catch
			{
				throw new NotSupportedException($"Update failed on symbol {symbolProfile.Symbol}.");
			}

			logger.LogDebug($"Updated symbol {symbolProfile.Symbol}");

			ClearCache();
		}

		public async Task SetMarketPrice(SymbolProfile symbolProfile, Money money, DateTime dateTime)
		{
			if (!settings.AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["marketPrice"] = money.Amount
			};

			var res = o.ToString();

			var r = await restCall.DoRestPut($"api/v1/admin/market-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}/{dateTime:yyyy-MM-dd}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"SetMarketPrice failed {symbolProfile.Symbol} {dateTime}");
			}

			logger.LogDebug($"SetMarketPrice symbol {symbolProfile.Symbol} {dateTime} @ {money.Amount}");
		}

		public async Task DeleteSymbol(SymbolProfile symbolProfile)
		{
			if (!settings.AllowAdminCalls)
			{
				return;
			}

			var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {symbolProfile.Symbol}");
			}

			logger.LogDebug($"Deleted symbol {symbolProfile.Symbol}");
		}

		public async Task SetSymbolAsBenchmark(SymbolProfile symbolProfile)
		{
			if (!settings.AllowAdminCalls)
			{
				return;
			}

			var currentBanchmarks = (await GetInfo()).BenchMarks!;
			if (Array.Exists(currentBanchmarks, x => x.Symbol == symbolProfile.Symbol))
			{
				return;
			}

			var o = new JObject
			{
				["datasource"] = symbolProfile.DataSource,
				["symbol"] = symbolProfile.Symbol

			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/benchmark/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Updating symbol failed to mark as a benchmark {symbolProfile.Symbol}");
			}

			logger.LogDebug($"Updated symbol to be a benchmark {symbolProfile.Symbol}");
		}

		public async Task GatherAllMarktData()
		{
			if (!settings.AllowAdminCalls)
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

			logger.LogDebug($"Gathering requested");
		}

		private void ClearCache()
		{
			memoryCache.Clear();
		}

	}
}
