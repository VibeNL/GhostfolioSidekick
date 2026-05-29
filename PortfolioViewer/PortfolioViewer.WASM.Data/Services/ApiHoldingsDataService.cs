using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// An <see cref="IHoldingsDataService"/> implementation that queries the API directly
	/// instead of reading from the local synced database.
	/// </summary>
	public class ApiHoldingsDataService(HttpClient httpClient, IServerConfigurationService serverConfigurationService) : ApiServiceBase, IHoldingsDataService
	{

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(CancellationToken cancellationToken = default)
		{
			return FetchHoldingsAsync("api/holdings", cancellationToken);
		}

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(int accountId, CancellationToken cancellationToken = default)
		{
			return FetchHoldingsAsync(accountId == 0 ? "api/holdings" : $"api/holdings/account/{accountId}", cancellationToken);
		}

		public async Task<HoldingDisplayModel?> GetHoldingAsync(string symbol, CancellationToken cancellationToken = default)
		{
			var response = await httpClient.GetAsync($"api/holdings/{Uri.EscapeDataString(symbol)}", cancellationToken);
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return null;
			}

			response.EnsureSuccessStatusCode();
			var dto = await response.Content.ReadFromJsonAsync<HoldingDisplayModelDto>(JsonOptions, cancellationToken);
			return dto == null ? null : MapToModel(dto);
		}

		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			var url = $"api/holdings/{Uri.EscapeDataString(symbol)}/price-history?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
			var dtos = await httpClient.GetFromJsonAsync<List<HoldingPriceHistoryPointDto>>(url, JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return [];
			}

			return dtos.Select(d => new HoldingPriceHistoryPoint
			{
				Date = d.Date,
				Price = d.Price,
				AveragePrice = d.AveragePrice,
			}).ToList();
		}

		public async Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			DateOnly startDate,
			DateOnly endDate,
			int? accountId,
			CancellationToken cancellationToken = default)
		{
			var accountParam = accountId.HasValue && accountId != 0 ? $"&accountId={accountId}" : string.Empty;
			var url = $"api/holdings/portfolio-value-history?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}{accountParam}";
			var dtos = await httpClient.GetFromJsonAsync<List<PortfolioValueHistoryPointDto>>(url, JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return [];
			}

			return dtos.Select(d => new PortfolioValueHistoryPoint
			{
				Date = d.Date,
				Value = d.Value,
				Invested = d.Invested,
				Balance = d.Balance,
			}).ToList();
		}

		private async Task<List<HoldingDisplayModel>> FetchHoldingsAsync(string url, CancellationToken cancellationToken)
		{
			var dtos = await httpClient.GetFromJsonAsync<List<HoldingDisplayModelDto>>(url, JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return [];
			}

			return dtos.Select(MapToModel).ToList();
		}

		private HoldingDisplayModel MapToModel(HoldingDisplayModelDto dto)
		{
			var currency = serverConfigurationService.PrimaryCurrency;
			return new HoldingDisplayModel
			{
				AssetClass = dto.AssetClass,
				AveragePrice = new Money(currency, dto.AveragePrice),
				Currency = dto.Currency,
				CurrentPrice = new Money(currency, dto.CurrentPrice),
				CurrentValue = new Money(currency, dto.CurrentValue),
				GainLoss = new Money(currency, dto.GainLoss),
				GainLossPercentage = dto.GainLossPercentage,
				Name = dto.Name,
				Quantity = dto.Quantity,
				Sector = dto.Sector,
				Symbols = dto.Symbols,
				Weight = dto.Weight,
			};
		}

		private sealed class HoldingDisplayModelDto
		{
			public List<string> Symbols { get; set; } = [];
			public string Name { get; set; } = string.Empty;
			public decimal CurrentValue { get; set; }
			public decimal Quantity { get; set; }
			public decimal AveragePrice { get; set; }
			public decimal CurrentPrice { get; set; }
			public decimal GainLoss { get; set; }
			public decimal GainLossPercentage { get; set; }
			public decimal Weight { get; set; }
			public string Sector { get; set; } = string.Empty;
			public string AssetClass { get; set; } = string.Empty;
			public string Currency { get; set; } = "EUR";
		}

		private sealed class HoldingPriceHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public decimal Price { get; set; }
			public decimal AveragePrice { get; set; }
		}

		private sealed class PortfolioValueHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public decimal Value { get; set; }
			public decimal Invested { get; set; }
			public decimal Balance { get; set; }
		}

			}
		}
