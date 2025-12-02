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
		private const string BaseUrl = "https://www.dividendmax.com";
		private const string SuggestEndpoint = "/suggest.json";

		public string DataSource => Datasource.DividendMax;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var searchTerms = GetSearchTerms(symbolIdentifiers);
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
						[.. symbolIdentifiers.Select(x => x.Identifier)],
						[result.Ticker, result.Name])
				})
				.OrderByDescending(x => x.Score)
				.FirstOrDefault();

			if (bestMatch == null || bestMatch.Score <= 0)
			{
				return null;
			}

			// Build SymbolProfile from best match
			var profile = new SymbolProfile
			{
				Symbol = bestMatch.Result.Ticker,
				Name = bestMatch.Result.Name,
				DataSource = DataSource,
				Identifiers = [.. symbolIdentifiers.Select(id => id.Identifier)],
				WebsiteUrl = $"{BaseUrl}{bestMatch.Result.Path}"
			};

			return profile;
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
			return await httpClient.GetFromJsonAsync<List<SuggestResult>>(suggestUrl);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Serializing")]
		private sealed class SuggestResult
		{
			public required long Id { get; set; } // Unique identifier

			public required string Name { get; set; } // Full name

			public required string Path { get; set; } // e.g. /stocks/us/apple-inc-aapl

			public required string Ticker { get; set; } // Symbol

			public required string Flag { get; set; } // Country code
		}
	}
}
