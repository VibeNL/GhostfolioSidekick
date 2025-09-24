using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class TransactionService(
			DatabaseContext databaseContext
		) : ITransactionService
	{
		public async Task<List<TransactionDisplayModel>> GetTransactionsAsync(
			Currency targetCurrency,
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			CancellationToken cancellationToken = default)
		{
			var baseQuery = databaseContext.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= startDate.ToDateTime(TimeOnly.MinValue) && a.Date <= endDate.ToDateTime(TimeOnly.MinValue));

			if (accountId > 0)
			{
				baseQuery = baseQuery.Where(a => a.Account.Id == accountId);
			}

			if (!string.IsNullOrWhiteSpace(symbol))
			{
				baseQuery = baseQuery.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(x => x.Symbol == symbol));
			}

			// Project to anonymous type with raw data only - avoid Money objects in server-side projection
			var serverSideData = await baseQuery.OrderByDescending(a => a.Date)
				.ThenByDescending(a => a.Id)
				.Select(a => new
				{
					ActivityId = a.Id,
					ActivityDate = a.Date,
					ActivityType = a.GetType().Name, // Use Name instead of ToString() for better performance
					ActivityDescription = a.Description ?? "",
					ActivityTransactionId = a.TransactionId ?? "",
					AccountName = a.Account.Name ?? "",
					SymbolCode = a.Holding != null ? a.Holding.SymbolProfiles.FirstOrDefault().Symbol : null,
					SymbolName = a.Holding != null ? a.Holding.SymbolProfiles.FirstOrDefault().Name : null,
					// Cast to ActivityWithQuantityAndUnitPrice and get raw properties
					IsQuantityActivity = a is ActivityWithQuantityAndUnitPrice,
					Quantity = a is ActivityWithQuantityAndUnitPrice ? ((ActivityWithQuantityAndUnitPrice)a).Quantity : (decimal?)null,
					UnitPriceAmount = a is ActivityWithQuantityAndUnitPrice ? EF.Property<decimal?>(((ActivityWithQuantityAndUnitPrice)a).UnitPrice, "Amount") : null,
					UnitPriceCurrencySymbol = a is ActivityWithQuantityAndUnitPrice ? ((ActivityWithQuantityAndUnitPrice)a).UnitPrice.Currency.Symbol : null,
					Amount = a is ActivityWithAmount ? EF.Property<decimal?>(((ActivityWithAmount)a).Amount, "Amount") : null,
					AmountCurrencySymbol = a is ActivityWithAmount ? ((ActivityWithAmount)a).Amount.Currency.Symbol : null,
				})
				.ToListAsync(cancellationToken);

			// Transform to TransactionDisplayModel on client-side with safe Money construction
			var result = serverSideData.Select(data => new TransactionDisplayModel
			{
				Id = data.ActivityId,
				Date = data.ActivityDate,
				Type = data.ActivityType.Replace("Proxy", ""),
				Symbol = data.SymbolCode ?? "",
				Name = data.SymbolName ?? "",
				Description = data.ActivityDescription,
				TransactionId = data.ActivityTransactionId,
				AccountName = data.AccountName,
				Quantity = data.IsQuantityActivity ? data.Quantity : null,
				UnitPrice = data.IsQuantityActivity && data.UnitPriceAmount.HasValue && !string.IsNullOrEmpty(data.UnitPriceCurrencySymbol)
					? new Money(Currency.GetCurrency(data.UnitPriceCurrencySymbol), data.UnitPriceAmount.Value)
					: null,
				Currency = data.UnitPriceCurrencySymbol ?? "",
				Amount = data.Amount.HasValue && !string.IsNullOrEmpty(data.AmountCurrencySymbol)
					? new Money(Currency.GetCurrency(data.AmountCurrencySymbol), data.Amount.Value)
					: null,
			}).ToList();

			return result;
		}
	}
}
