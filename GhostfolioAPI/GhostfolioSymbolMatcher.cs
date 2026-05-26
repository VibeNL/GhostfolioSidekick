using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.Cache;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Polly.Retry;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioSymbolMatcher : ISymbolMatcher
	{
		private readonly IApiWrapper apiWrapper;
		private readonly ILogger<GhostfolioSymbolMatcher> logger;
		private readonly IExternalDataCacheService cacheService;

		public GhostfolioSymbolMatcher(
			IApplicationSettings settings,
			IApiWrapper apiWrapper,
			ILogger<GhostfolioSymbolMatcher> logger,
			IExternalDataCacheService cacheService)
		{
			ArgumentNullException.ThrowIfNull(settings);
			this.apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
			this.logger = logger;
			this.cacheService = cacheService;
			SortorderDataSources = [.. settings.ConfigurationInstance.Settings.DataProviderPreference.Split(',').Select(x => x.ToUpperInvariant())];
		}

		public string DataSource => Datasource.GHOSTFOLIO;

		private List<string> SortorderDataSources { get; set; }

		public bool AllowedForDeterminingHolding => true;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			try
			{
				if (symbolIdentifiers == null || symbolIdentifiers.Length == 0)
				{
					return null;
				}

				string cacheKey = string.Join(";", symbolIdentifiers.Select(x => x.Identifier).Order());
				return string.IsNullOrWhiteSpace(cacheKey)
				? null
				: await cacheService.GetOrAddAsync<SymbolProfile>(
					CacheKey.CreateSymbolProfile(Source.Ghostfolio, cacheKey),
				async () =>
				{
					AsyncRetryPolicy retryPolicy = GhostfolioSidekick.ExternalDataProvider.RetryPolicyHelper.GetRetryPolicy(logger);
					return (await retryPolicy.ExecuteAsync(async () =>
					{
						foreach (PartialSymbolIdentifier identifier in symbolIdentifiers)
						{
							List<string> ids = [identifier.Identifier];

							if (identifier.AllowedAssetSubClasses?.Contains(AssetSubClass.CryptoCurrency) ?? false)
							{
								ids.Add($"{identifier.Identifier}USD");
								ids.Add(CryptoMapper.Instance.GetFullname(identifier.Identifier));
							}

							ids = [.. ids.Distinct(StringComparer.InvariantCultureIgnoreCase)];

							SymbolProfile? symbol = await FindByDataProvider(
								ids,
								null,
								identifier.AllowedAssetClasses?.ToArray(),
								identifier.AllowedAssetSubClasses?.ToArray(),
								false);

							if (symbol != null)
							{
								return symbol;
							}
						}
						return null;
					}))!;
				})!;
			}
			catch
			{
				return null;
			}
		}

		private async Task<SymbolProfile?> FindByDataProvider(IEnumerable<string> ids, Currency? expectedCurrency, AssetClass[]? expectedAssetClass, AssetSubClass[]? expectedAssetSubClass, bool includeIndexes)
		{
			List<string> identifiers = ids.ToList();
			List<SymbolProfile> allAssets = [];

			foreach (string identifier in identifiers)
			{
				for (int i = 0; i < 5; i++) // Bug Ghostfolio, sometimes it just returns 0 items.
				{
					List<SymbolProfile> assets = await apiWrapper.GetSymbolProfile(identifier, includeIndexes);

					if (assets != null && assets.Count > 0)
					{
						allAssets.AddRange(assets);
						break;
					}
				}
			}

			List<SymbolProfile> filteredAsset = allAssets
				.Where(x => x != null)
				.Select(FixYahooCrypto)
				.Where(x => expectedAssetClass == null || expectedAssetClass.Length == 0 || expectedAssetClass.Contains(x.AssetClass))
					.Where(x => expectedAssetSubClass == null || expectedAssetSubClass.Length == 0 || expectedAssetSubClass.Contains(x.AssetSubClass.GetValueOrDefault()))
				.OrderBy(x => identifiers.Exists(y => MatchId(x, y)) ? 0 : 1)
				.ThenByDescending(x => FussyMatch(identifiers, x))
				.ThenBy(x => string.Equals(x.Currency.Symbol, expectedCurrency?.Symbol, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
				.ThenBy(x => new[] { Currency.EUR.Symbol, Currency.USD.Symbol, Currency.GBP.Symbol, Currency.GBp.Symbol }.Contains(x.Currency.Symbol) ? 0 : 1) // prefer well known currencies
				.ThenBy(x =>
				{
					int index = SortorderDataSources.IndexOf(x.DataSource.Replace(DataSource + "_", string.Empty).ToString().ToUpperInvariant());
					if (index < 0)
					{
						index = int.MaxValue;
					}

					return index;
				}) // prefer Yahoo above Coingecko due to performance
				.ThenBy(x => x.Name?.Length ?? int.MaxValue)
				.ToList();
			return filteredAsset.FirstOrDefault();
		}

		private static int FussyMatch(List<string> identifiers, SymbolProfile profile)
		{
			int match = identifiers.Max(x => Math.Max(FuzzySharp.Fuzz.Ratio(x, profile?.Name ?? string.Empty), FuzzySharp.Fuzz.Ratio(x, profile?.Symbol ?? string.Empty)));
			return match;
		}

		private static SymbolProfile FixYahooCrypto(SymbolProfile x)
		{
			// Workaround for bug Ghostfolio
			if (x.AssetSubClass == AssetSubClass.CryptoCurrency && Datasource.YAHOO.ToString().Equals(x.DataSource, StringComparison.InvariantCultureIgnoreCase) && x.Symbol.Length >= 6)
			{
				string t = x.Symbol;
				x.Symbol = string.Concat(t.AsSpan(0, t.Length - 3), "-", t.AsSpan(t.Length - 3, 3));
			}

			return x;
		}

		private static bool MatchId(SymbolProfile x, string id)
		{
			if (string.Equals(x.ISIN, id, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			if (string.Equals(x.Symbol, id, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			return x.AssetSubClass == AssetSubClass.CryptoCurrency &&
				string.Equals(x.Symbol, id + "USD", StringComparison.InvariantCultureIgnoreCase) || (x.AssetSubClass == AssetSubClass.CryptoCurrency &&
				string.Equals(x.Symbol, id.Replace(" ", "-"), StringComparison.InvariantCultureIgnoreCase)) || string.Equals(x.Name, id, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
