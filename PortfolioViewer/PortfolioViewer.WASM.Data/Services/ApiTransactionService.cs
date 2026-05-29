using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// An <see cref="ITransactionService"/> implementation that queries the API directly.
	/// </summary>
	public class ApiTransactionService(HttpClient httpClient) : ApiServiceBase, ITransactionService
	{
		public async Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(
			TransactionQueryParameters parameters,
			CancellationToken cancellationToken = default)
		{
			var requestBody = new TransactionQueryDto
			{
				StartDate = parameters.StartDate,
				EndDate = parameters.EndDate,
				AccountId = parameters.AccountId,
				Symbol = parameters.Symbol,
				TransactionTypes = parameters.TransactionTypes,
				SearchText = parameters.SearchText,
				SortColumn = parameters.SortColumn,
				SortAscending = parameters.SortAscending,
				PageNumber = parameters.PageNumber,
				PageSize = parameters.PageSize,
			};

			var response = await httpClient.PostAsJsonAsync("api/transactions/paginated", requestBody, JsonOptions, cancellationToken);
			response.EnsureSuccessStatusCode();

			var dto = await response.Content.ReadFromJsonAsync<PaginatedTransactionResultDto>(JsonOptions, cancellationToken);
			if (dto == null)
			{
				return new PaginatedTransactionResult();
			}

			return new PaginatedTransactionResult
			{
				Transactions = dto.Transactions.Select(MapTransaction).ToList(),
				TotalCount = dto.TotalCount,
				PageNumber = dto.PageNumber,
				PageSize = dto.PageSize,
				TransactionTypeBreakdown = dto.TransactionTypeBreakdown,
				AccountBreakdown = dto.AccountBreakdown,
			};
		}

		public async Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default)
		{
			var result = await httpClient.GetFromJsonAsync<List<string>>("api/transactions/types", JsonOptions, cancellationToken);
			return result ?? [];
		}

		private static TransactionDisplayModel MapTransaction(TransactionDisplayModelDto dto)
		{
			Currency? unitPriceCurrency = string.IsNullOrEmpty(dto.UnitPriceCurrency) ? null : Currency.GetCurrency(dto.UnitPriceCurrency);
			Currency? amountCurrency = string.IsNullOrEmpty(dto.AmountCurrency) ? null : Currency.GetCurrency(dto.AmountCurrency);
			Currency? totalCurrency = string.IsNullOrEmpty(dto.TotalValueCurrency) ? null : Currency.GetCurrency(dto.TotalValueCurrency);
			Currency? feeCurrency = string.IsNullOrEmpty(dto.FeeCurrency) ? null : Currency.GetCurrency(dto.FeeCurrency);
			Currency? taxCurrency = string.IsNullOrEmpty(dto.TaxCurrency) ? null : Currency.GetCurrency(dto.TaxCurrency);

			return new TransactionDisplayModel
			{
				Id = dto.Id,
				Date = dto.Date,
				Type = dto.Type,
				Symbol = dto.Symbol,
				Name = dto.Name,
				Description = dto.Description,
				TransactionId = dto.TransactionId,
				AccountName = dto.AccountName,
				Quantity = dto.Quantity,
				Currency = dto.UnitPriceCurrency,
				UnitPrice = dto.UnitPriceAmount.HasValue && unitPriceCurrency != null ? new Money(unitPriceCurrency, dto.UnitPriceAmount.Value) : null,
				Amount = dto.AmountValue.HasValue && amountCurrency != null ? new Money(amountCurrency, dto.AmountValue.Value) : null,
				TotalValue = dto.TotalValueAmount.HasValue && totalCurrency != null ? new Money(totalCurrency, dto.TotalValueAmount.Value) : null,
				Fee = dto.FeeAmount.HasValue && feeCurrency != null ? new Money(feeCurrency, dto.FeeAmount.Value) : null,
				Tax = dto.TaxAmount.HasValue && taxCurrency != null ? new Money(taxCurrency, dto.TaxAmount.Value) : null,
			};
		}

		private sealed class TransactionQueryDto
		{
			public DateOnly StartDate { get; set; }
			public DateOnly EndDate { get; set; }
			public int AccountId { get; set; }
			public string Symbol { get; set; } = string.Empty;
			public List<string> TransactionTypes { get; set; } = [];
			public string SearchText { get; set; } = string.Empty;
			public string SortColumn { get; set; } = "Date";
			public bool SortAscending { get; set; } = true;
			public int PageNumber { get; set; } = 1;
			public int PageSize { get; set; } = 25;
		}

		private sealed class TransactionDisplayModelDto
		{
			public long Id { get; set; }
			public DateTime Date { get; set; }
			public string Type { get; set; } = string.Empty;
			public string? Symbol { get; set; }
			public string? Name { get; set; }
			public string Description { get; set; } = string.Empty;
			public string TransactionId { get; set; } = string.Empty;
			public string AccountName { get; set; } = string.Empty;
			public decimal? Quantity { get; set; }
			public decimal? UnitPriceAmount { get; set; }
			public string UnitPriceCurrency { get; set; } = string.Empty;
			public decimal? AmountValue { get; set; }
			public string AmountCurrency { get; set; } = string.Empty;
			public decimal? TotalValueAmount { get; set; }
			public string TotalValueCurrency { get; set; } = string.Empty;
			public decimal? FeeAmount { get; set; }
			public string FeeCurrency { get; set; } = string.Empty;
			public decimal? TaxAmount { get; set; }
			public string TaxCurrency { get; set; } = string.Empty;
		}

		private sealed class PaginatedTransactionResultDto
		{
			public List<TransactionDisplayModelDto> Transactions { get; set; } = [];
			public int TotalCount { get; set; }
			public int PageNumber { get; set; }
			public int PageSize { get; set; }
			public Dictionary<string, int> TransactionTypeBreakdown { get; set; } = [];
			public Dictionary<string, int> AccountBreakdown { get; set; } = [];
		}
	}
}
