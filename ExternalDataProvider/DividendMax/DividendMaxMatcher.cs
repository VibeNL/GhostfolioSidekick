using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Utilities;
using System.Net.Http;
using System.Net.Http.Json;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.ExternalDataProvider.DividendMax
{
	public class DividendMaxMatcher(IHttpClientFactory httpClientFactory) : ISymbolMatcher
	{
		// Suppression: Hardcoded absolute paths/URIs are required for external API endpoints.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA2234:AvoidHardcodedUris", Justification = "External API endpoint is fixed and must be hardcoded.")]
		private const string BaseUrl = "https://www.dividendmax.com";
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA2234:AvoidHardcodedUris", Justification = "External API endpoint is fixed and must be hardcoded.")]
		private const string SuggestEndpoint = "/suggest.json";

		public string DataSource => Datasource.DividendMax;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var cleanedIdentifiers = symbolIdentifiers
				.Where(x => HasDividends(x))
				.ToArray();

			if (cleanedIdentifiers.Length == 0)
			{
				return null;
			}

			var searchTerms = GetSearchTerms(cleanedIdentifiers);
			var searchResults = new List<SuggestResult>();
			foreach (var searchTerm in searchTerms)
			{
				var suggestResponse = await GetSuggestResponse(searchTerm);
				if (suggestResponse != null && suggestResponse.Count > 0)
				{
					searchResults.AddRange(suggestResponse);
				}
			}

			if (searchResults.Count == 0)
			{
				return null;
			}

			// Semantic matching and sort on score
			var bestMatch = searchResults
				.Select(result => new
				{
					Result = result,
					Score = SemanticMatcher.CalculateSemanticMatchScore(
						[.. cleanedIdentifiers.Select(x => x.Identifier)],
						[result.Ticker, result.CleanedName ?? result.Name])
				})
				.OrderByDescending(x => x.Score)
				.ThenByDescending(x => x.Result.Name.Length)
				.First();

			if (bestMatch == null || bestMatch.Score <= 0) // Minimum score threshold
			{
				return null;
			}

			// Build SymbolProfile from best match
			var profile = new SymbolProfile(
				symbol: bestMatch.Result.Ticker,
				name: bestMatch.Result.Name,
				dataSource: DataSource,
				currency: Currency.NONE,
				identifiers: [.. cleanedIdentifiers.Select(id => id.Identifier)],
				assetClass: AssetClass.Equity,
				assetSubClass: null,
				countries: [],
				sectors: [])
			{
				WebsiteUrl = $"{BaseUrl}{bestMatch.Result.Path}"
			};

			return profile;
		}

		private static bool HasDividends(PartialSymbolIdentifier identifier)
		{
			if (identifier.AllowedAssetClasses?.Contains(AssetClass.Equity) ?? false)
			{
				return true;
			}

			// If Empty list of null, assume all asset classes are allowed otherwise false
			if (identifier.AllowedAssetClasses == null || identifier.AllowedAssetClasses.Count == 0)
			{
				return true;
			}

			return false;
		}

		private static List<string> GetSearchTerms(PartialSymbolIdentifier[] partialSymbolIdentifiers)
		{
			var searchTerms = partialSymbolIdentifiers.Select(id => id.Identifier).ToList();

			return [.. searchTerms
				.FilterInvalidNames()
				.FilterEmpty()
				.Distinct()
				];
		}

		private async Task<List<SuggestResult>?> GetSuggestResponse(string searchTerm)
		{
			var suggestUrl = $"{BaseUrl}{SuggestEndpoint}?q={searchTerm}";

			using var httpClient = httpClientFactory.CreateClient();
			var r = await httpClient.GetFromJsonAsync<List<SuggestResult>>(suggestUrl);

			// Remove delisted entries
			if (r == null)
			{
				return null;
			}

			r = [.. r.Where(x => !x.Name.Contains("(delisted)", StringComparison.OrdinalIgnoreCase))];

			// Remove terms in the names like co., corp., inc., ltd.
			r = [.. r.Select(x =>
			{
				x.CleanedName = SymbolNameCleaner.CleanSymbolName(x.Name);
				return x;
			})];


			return r;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Required for serialization")]
		private sealed class SuggestResult
		{
			public required long Id { get; set; } // Unique identifier

			public required string Name { get; set; } // Full name

			public required string Path { get; set; } // e.g. /stocks/us/apple-inc-aapl

			public required string Ticker { get; set; } // Symbol

			public required string Flag { get; set; } // Country code

			public string? CleanedName { get; internal set; } // Cleaned name
		}
	}
}
