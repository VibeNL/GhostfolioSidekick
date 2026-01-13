using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Utilities;
using System.Net.Http.Json;
using GhostfolioSidekick.Model;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.ExternalDataProvider.TipRanks
{
	public class TipRanksMatcher(IHttpClientFactory httpClientFactory) : ISymbolMatcher
	{
		[SuppressMessage("Sonar", "S1075:URIs should not be hardcoded", Justification = "External API endpoint is stable and required for integration")]
		private const string SearchUrl = "https://autocomplete.tipranks.com/api/autocomplete/search?name=";

		private const string ForecastUrl = "https://www.tipranks.com/stocks/";
		private const string PostFixForecastUrl = "/forecast";

		public string DataSource => Datasource.TIPRANKS;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var cleanedIdentifiers = symbolIdentifiers
				.Where(x => IsIndividualStock(x))
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
			var sortedResults = searchResults
				.Where(x => x.Category == "ticker") // Only stocks for now
				.Select(result => new
				{
					Result = result,
					Score = SemanticMatcher.CalculateSemanticMatchScore(
						[.. cleanedIdentifiers.Select(x => x.Identifier)],
						[result.CleanedName ?? result.Label])
				})
				.OrderByDescending(x => x.Score) // Take the highest score
				.ThenBy(x => x.Result.Value.Length) // Take the shorter Ticker
				.ThenByDescending(x => x.Result.Label.Length) // Take the longer name
				.ToList();
			var bestMatch = sortedResults 
				.FirstOrDefault();

			if (bestMatch == null || bestMatch.Score <= 0) // Minimum score threshold
			{
				return null;
			}

			// Build SymbolProfile from best match
			var profile = new SymbolProfile(
				symbol: bestMatch.Result.Value,
				name: bestMatch.Result.Label,
				dataSource: DataSource,
				currency: Currency.NONE,
				identifiers: [.. cleanedIdentifiers.Select(id => id.Identifier)],
				assetClass: AssetClass.Equity,
				assetSubClass: null,
				countries: [],
				sectors: [])
			{
				WebsiteUrl = $"{ForecastUrl}{bestMatch.Result.Value}{PostFixForecastUrl}"
			};

			return profile;
		}

		private static bool IsIndividualStock(PartialSymbolIdentifier identifier)
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
			var suggestUrl = $"{SearchUrl}{searchTerm}";

			using var httpClient = httpClientFactory.CreateClient();
			var r = await httpClient.GetFromJsonAsync<List<SuggestResult>>(suggestUrl);

			// Remove delisted entries
			if (r == null)
			{
				return null;
			}

			r = [.. r.Where(x => !x.Label.Contains("(delisted)", StringComparison.OrdinalIgnoreCase))];

			// Remove terms in the names like co., corp., inc., ltd.
			r = [.. r.Select(x =>
			{
				x.CleanedName = SymbolNameCleaner.CleanSymbolName(x.Label);
				return x;
			})];

			return r;
		}

		[SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Required for serialization")]
		[SuppressMessage("CodeQuality", "S1144:Unused private types or members should be removed", Justification = "Used for deserialization")]
		[SuppressMessage("Style", "S1104:Fields should not have public accessibility", Justification = "DTO for JSON deserialization")]
		private sealed class SuggestResult
		{
			// [{"label":"ASR Nederland N.V","ticker":null,"value":"NL:ASRNL","category":"ticker","uid":"3da53095","countryId":14,"extraData":{"market":"Amsterdam","marketCap":12544542032},"stockType":12,"followers":14,"keywords":[]}]

			public string Label { get; set; } = string.Empty;

			public string Value { get; set; } = string.Empty;

			public string Category { get; set; } = string.Empty;

			public string Uid { get; set; } = string.Empty;

			public string CleanedName { get; set; } = string.Empty;

			public override string ToString()
			{
				return $"{Label} ({Value})";
			}
		}
	}
}
