using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioSymbolMatcher : ISymbolMatcher
	{
		private readonly IApiWrapper apiWrapper;

		public GhostfolioSymbolMatcher(IApplicationSettings settings, IApiWrapper apiWrapper)
		{
			ArgumentNullException.ThrowIfNull(settings);
			this.apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
			SortorderDataSources = [.. settings.ConfigurationInstance.Settings.DataProviderPreference.Split(',').Select(x => x.ToUpperInvariant())];
		}

		public string DataSource => Datasource.GHOSTFOLIO;

		private List<string> SortorderDataSources { get; set; }

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			if (symbolIdentifiers == null || symbolIdentifiers.Length == 0)
			{
				return null;
			}

			foreach (var identifier in symbolIdentifiers)
			{
				var symbol = await FindByDataProvider(
					new[] { identifier.Identifier },
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
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Needed for the functionality, splitting will not solve anything")]
        private async Task<SymbolProfile?> FindByDataProvider(IEnumerable<string> ids, Currency? expectedCurrency, AssetClass[]? expectedAssetClass, AssetSubClass[]? expectedAssetSubClass, bool includeIndexes)
        {
            var identifiers = ids.ToList();
            var allAssets = new List<SymbolProfile>();

            foreach (var identifier in identifiers)
            {
                for (var i = 0; i < 5; i++)
                {
                    var assets = await apiWrapper.GetSymbolProfile(identifier, includeIndexes);

                    if (assets != null && assets.Count > 0)
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
                //.ThenBy(x => x.AssetSubClass == AssetSubClass.CryptoCurrency && x.Name?.Contains("[OLD]") ? 1 : 0)
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

		private static int FussyMatch(List<string> identifiers, SymbolProfile profile)
		{
			return identifiers.Max(x => Math.Max(FuzzySharp.Fuzz.Ratio(x, profile?.Name ?? string.Empty), FuzzySharp.Fuzz.Ratio(x, profile?.Symbol ?? string.Empty)));
		}

		private static SymbolProfile FixYahooCrypto(SymbolProfile x)
		{
			// Workaround for bug Ghostfolio
			if (x.AssetSubClass == AssetSubClass.CryptoCurrency && Model.Symbols.Datasource.YAHOO.ToString().Equals(x.DataSource, StringComparison.InvariantCultureIgnoreCase) && x.Symbol.Length >= 6)
			{
				var t = x.Symbol;
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
	}
}
