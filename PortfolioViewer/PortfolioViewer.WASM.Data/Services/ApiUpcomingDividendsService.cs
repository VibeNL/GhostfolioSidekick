using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// An <see cref="IUpcomingDividendsService"/> implementation that queries the API directly.
	/// </summary>
	public class ApiUpcomingDividendsService(HttpClient httpClient) : ApiServiceBase, IUpcomingDividendsService
	{
		public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
		{
			var dtos = await httpClient.GetFromJsonAsync<List<UpcomingDividendDto>>("api/upcomingdividends", JsonOptions);
			if (dtos == null)
			{
				return [];
			}

			return dtos.Select(d => new UpcomingDividendModel
			{
				Symbol = d.Symbol,
				CompanyName = d.CompanyName,
				ExDate = d.ExDate,
				PaymentDate = d.PaymentDate,
				Amount = d.Amount,
				Currency = d.Currency,
				DividendPerShare = d.DividendPerShare,
				AmountPrimaryCurrency = d.AmountPrimaryCurrency,
				PrimaryCurrency = d.PrimaryCurrency,
				DividendPerSharePrimaryCurrency = d.DividendPerSharePrimaryCurrency,
				Quantity = d.Quantity,
				IsPredicted = d.IsPredicted,
			}).ToList();
		}

		private sealed class UpcomingDividendDto
		{
			public string Symbol { get; set; } = string.Empty;
			public string CompanyName { get; set; } = string.Empty;
			public DateOnly ExDate { get; set; }
			public DateOnly PaymentDate { get; set; }
			public decimal Amount { get; set; }
			public string Currency { get; set; } = string.Empty;
			public decimal DividendPerShare { get; set; }
			public decimal? AmountPrimaryCurrency { get; set; }
			public string PrimaryCurrency { get; set; } = string.Empty;
			public decimal? DividendPerSharePrimaryCurrency { get; set; }
			public decimal Quantity { get; set; }
			public bool IsPredicted { get; set; }
		}
	}
}
