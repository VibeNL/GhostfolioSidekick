using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// An <see cref="IDataIssuesService"/> implementation that queries the API directly.
	/// </summary>
	public class ApiDataIssuesService(HttpClient httpClient) : ApiServiceBase, IDataIssuesService
	{
		public async Task<List<DataIssueDisplayModel>> GetActivitiesWithoutHoldingsAsync(CancellationToken cancellationToken = default)
		{
			var dtos = await httpClient.GetFromJsonAsync<List<DataIssueDisplayModelDto>>("api/dataissues", JsonOptions, cancellationToken);
			if (dtos == null)
			{
				return [];
			}

			return dtos.Select(dto =>
			{
				Currency? unitPriceCurrency = string.IsNullOrEmpty(dto.UnitPriceCurrency) ? null : Currency.GetCurrency(dto.UnitPriceCurrency);
				Currency? amountCurrency = string.IsNullOrEmpty(dto.AmountCurrency) ? null : Currency.GetCurrency(dto.AmountCurrency);

				return new DataIssueDisplayModel
				{
					Id = dto.Id,
					IssueType = dto.IssueType,
					Description = dto.Description,
					Date = dto.Date,
					AccountName = dto.AccountName,
					ActivityType = dto.ActivityType,
					Symbol = dto.Symbol,
					SymbolIdentifiers = dto.SymbolIdentifiers,
					Quantity = dto.Quantity,
					UnitPrice = dto.UnitPriceAmount.HasValue && unitPriceCurrency != null ? new Money(unitPriceCurrency, dto.UnitPriceAmount.Value) : null,
					Amount = dto.AmountValue.HasValue && amountCurrency != null ? new Money(amountCurrency, dto.AmountValue.Value) : null,
					TransactionId = dto.TransactionId,
					ActivityDescription = dto.ActivityDescription,
					Severity = dto.Severity,
				};
			}).ToList();
		}

		private sealed class DataIssueDisplayModelDto
		{
			public long Id { get; set; }
			public string IssueType { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public DateTime Date { get; set; }
			public string AccountName { get; set; } = string.Empty;
			public string ActivityType { get; set; } = string.Empty;
			public string? Symbol { get; set; }
			public string? SymbolIdentifiers { get; set; }
			public decimal? Quantity { get; set; }
			public decimal? UnitPriceAmount { get; set; }
			public string UnitPriceCurrency { get; set; } = string.Empty;
			public decimal? AmountValue { get; set; }
			public string AmountCurrency { get; set; } = string.Empty;
			public string TransactionId { get; set; } = string.Empty;
			public string? ActivityDescription { get; set; }
			public string Severity { get; set; } = "Warning";
		}
	}
}
