using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.ExternalDataProvider.TipRanks
{
	public class TipRanksScraper(
		ILogger<TipRanksScraper> logger,
		HttpClient httpClient) : ITargetPriceRepository
	{
		// Example https://www.tipranks.com/stocks/nl:asrnl/stock-forecast/payload.json

		public async Task<PriceTarget?> GetPriceTarget(SymbolProfile symbol)
		{
			if (symbol == null || symbol.WebsiteUrl == null || symbol.DataSource != Datasource.TIPRANKS)
			{
				return null;
			}

			var uri = new Uri(symbol.WebsiteUrl); // https://www.tipranks.com/stocks/nl:asrnl
			var segments = uri.Segments;
			if (segments.Length < 3)
			{
				logger.LogWarning("Invalid TipRanks URL format: {Url}", symbol.WebsiteUrl);
				return null;
			}

			var stockIdentifier = segments[2].TrimEnd('/'); // nl:asrnl
			return await GetPriceTargetFromApi(stockIdentifier);
		}

		private async Task<PriceTarget?> GetPriceTargetFromApi(string stockIdentifier)
		{
			// Construct the TipRanks API URL
			var apiUrl = $"https://www.tipranks.com/stocks/{stockIdentifier}/stock-forecast/payload.json";

			// Make the HTTP request
			var response = await httpClient.GetAsync(apiUrl);

			if (!response.IsSuccessStatusCode)
			{
				// Return empty PriceTarget if API call fails
				logger.LogWarning("Failed to fetch data from TipRanks API for {StockIdentifier}. Status Code: {StatusCode}", stockIdentifier, response.StatusCode);
				return null;
			}

			var jsonContent = await response.Content.ReadAsStringAsync();

			if (string.IsNullOrWhiteSpace(jsonContent))
			{
				logger.LogWarning("Empty response from TipRanks API for {StockIdentifier}", stockIdentifier);
				return null;
			}

			// Deserialize the JSON response
			var apiResponse = System.Text.Json.JsonSerializer.Deserialize<TipRanksApiResponse>(jsonContent);

			if (apiResponse?.Models?.Stocks == null || !apiResponse.Models.Stocks.Any())
			{
				logger.LogWarning("No stock data found in TipRanks API response for {StockIdentifier}", stockIdentifier);
				return null;
			}

			// Find the stock with matching ID (case-insensitive)
			var stockData = apiResponse.Models.Stocks.FirstOrDefault(s => 
				string.Equals(s.Id, stockIdentifier, StringComparison.OrdinalIgnoreCase));

			if (stockData?.AnalystRatings?.All == null)
			{
				logger.LogWarning("No analyst ratings found for {StockIdentifier}", stockIdentifier);
				return null;
			}

			// Map the API response to PriceTarget model
			return MapToPriceTarget(stockData.AnalystRatings.All);
		}

		private static PriceTarget MapToPriceTarget(TipRanksAnalystRatings analystRatings)
		{
			// Parse currency, default to USD if not available or invalid
			var currency = Model.Currency.USD;
			// Note: TipRanks API doesn't seem to provide currency info in the new format,
			// so we'll keep USD as default

			// Map consensus rating ID to AnalystRating enum
			var rating = MapConsensusRating(analystRatings.Id);

			return new PriceTarget
			{
				HighestTargetPrice = new Model.Money(currency, analystRatings.HighPriceTarget),
				AverageTargetPrice = new Model.Money(currency, analystRatings.PriceTarget?.Value ?? 0),
				LowestTargetPrice = new Model.Money(currency, analystRatings.LowPriceTarget),
				Rating = rating,
				NumberOfBuys = analystRatings.Buy,
				NumberOfHolds = analystRatings.Hold,
				NumberOfSells = analystRatings.Sell
			};
		}

		private static AnalystRating MapConsensusRating(string consensusRating)
		{
			return consensusRating?.ToLowerInvariant() switch
			{
				"strong buy" or "strongbuy" or "strongbuy" => AnalystRating.StrongBuy,
				"buy" or "moderatebuy" => AnalystRating.Buy,
				"hold" => AnalystRating.Hold,
				"sell" or "moderatesell" => AnalystRating.Sell,
				"strong sell" or "strongsell" => AnalystRating.StrongSell,
				_ => AnalystRating.Hold // Default to Hold for unknown ratings
			};
		}

		// TipRanks API response models
		internal class TipRanksApiResponse
		{
			[JsonPropertyName("models")]
			public TipRanksModels? Models { get; set; }
		}

		internal class TipRanksModels
		{
			[JsonPropertyName("stocks")]
			public List<TipRanksStock>? Stocks { get; set; }
		}

		internal class TipRanksStock
		{
			[JsonPropertyName("_id")]
			public string Id { get; set; } = string.Empty;

			[JsonPropertyName("analystRatings")]
			public TipRanksAnalystRatingsContainer? AnalystRatings { get; set; }
		}

		internal class TipRanksAnalystRatingsContainer
		{
			[JsonPropertyName("all")]
			public TipRanksAnalystRatings? All { get; set; }
		}

		internal class TipRanksAnalystRatings
		{
			[JsonPropertyName("buy")]
			public int Buy { get; set; }

			[JsonPropertyName("hold")]
			public int Hold { get; set; }

			[JsonPropertyName("sell")]
			public int Sell { get; set; }

			[JsonPropertyName("id")]
			public string Id { get; set; } = string.Empty;

			[JsonPropertyName("priceTarget")]
			public TipRanksPriceTargetValue? PriceTarget { get; set; }

			[JsonPropertyName("highPriceTarget")]
			public decimal HighPriceTarget { get; set; }

			[JsonPropertyName("lowPriceTarget")]
			public decimal LowPriceTarget { get; set; }
		}

		internal class TipRanksPriceTargetValue
		{
			[JsonPropertyName("value")]
			public decimal Value { get; set; }
		}
	}
}