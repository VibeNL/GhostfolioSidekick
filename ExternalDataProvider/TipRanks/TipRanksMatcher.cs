using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Utilities;
using System.Net.Http.Json;
using GhostfolioSidekick.Model;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.ExternalDataProvider.DividendMax
{
	public class TipRanksMatcher(IHttpClientFactory httpClientFactory) : ISymbolMatcher
	{
		/*https://autocomplete.tipranks.com/api/autocomplete/search?name=asrnl&topAnalysts=3&topInsiders=3&topInstitutions=3&topBloggers=3&topTickers=6&onlyStockTickers=false&ignoreFunds=false
		
			[
    {
        "label": "ASR Nederland N.V",
        "ticker": null,
        "value": "NL:ASRNL",
        "category": "ticker",
        "uid": "3da53095",
        "countryId": 14,
        "extraData": {
            "market": "Amsterdam",
            "marketCap": 12134264743

		},
        "stockType": 12,
        "followers": 13,
        "keywords": []
}
]
			
		https://www.tipranks.com/stocks/nl:asrnl/stock-forecast/payload.json
		*/

		[SuppressMessage("Sonar", "S1075:URIs should not be hardcoded", Justification = "External API endpoint is stable and required for integration")]
		private const string BaseUrl = "https://www.tipranks.com/";

		[SuppressMessage("Sonar", "S1075:URIs should not be hardcoded", Justification = "External API endpoint is stable and required for integration")]
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

			// If empty list or null, assume all asset classes are allowed; otherwise, false
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

		[SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Required for serialization")]
		[SuppressMessage("CodeQuality", "S1144:Unused private types or members should be removed", Justification = "Used for deserialization")]
		[SuppressMessage("Style", "S1104:Fields should not have public accessibility", Justification = "DTO for JSON deserialization")]
		private sealed class SuggestResult
		{
			public required string Name { get; set; } // Full name

			public required string Path { get; set; } // e.g. /stocks/us/apple-inc-aapl

			public required string Ticker { get; set; } // Symbol

			public required string Flag { get; set; } // Country code

			public string? CleanedName { get; internal set; } // Cleaned name
		}
	}
}
