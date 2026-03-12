using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services; // No change needed
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class TaxDetailsService
	{
		private readonly IAccountDataService _accountService;
		private readonly ITransactionService _transactionService;
		private readonly IServerConfigurationService _configService;

		public TaxDetailsService(IAccountDataService accountService, ITransactionService transactionService, IServerConfigurationService configService)
		{
			_accountService = accountService;
			_transactionService = transactionService;
			_configService = configService;
		}

		public async Task<List<int>> GetAvailableYearsAsync()
		{
			return await _transactionService.GetAvailableYearsAsync();
		}

		public async Task<List<TaxAccountDisplayModel>?> GetTaxAccountDetailsAsync(int year)
		{
			var accounts = await _accountService.GetTaxAccountDetailsAsync(year);
			if (accounts != null)
			{
				foreach (var account in accounts)
				{
					ComputeSymbolRows(account);
				}
			}
			return accounts;
		}

		public void ComputeSymbolRows(TaxAccountDisplayModel account)
		{
			var startSymbols = account.StartHoldings?.Select(h => h.Symbol).ToHashSet() ?? new HashSet<string?>();
			var endSymbols = account.EndHoldings?.Select(h => h.Symbol).ToHashSet() ?? new HashSet<string?>();
			var allSymbols = startSymbols.Union(endSymbols).OrderBy(s => s).ToList();
			var rows = new List<TaxAccountDisplayModel.SymbolRow>();
			decimal totalStart = 0m;
			decimal totalEnd = 0m;
			foreach (var symbol in allSymbols)
			{
				var start = account.StartHoldings?.FirstOrDefault(h => h.Symbol == symbol);
				var end = account.EndHoldings?.FirstOrDefault(h => h.Symbol == symbol);
				rows.Add(new TaxAccountDisplayModel.SymbolRow
				{
					Symbol = symbol ?? string.Empty,
					StartQuantity = start?.Quantity ?? 0,
					StartValue = start?.Value ?? 0,
					EndQuantity = end?.Quantity ?? 0,
					EndValue = end?.Value ?? 0
				});
				totalStart += start?.Value ?? 0;
				totalEnd += end?.Value ?? 0;
			}
			account.SymbolRows = rows;
			account.TotalStartValue = totalStart;
			account.TotalEndValue = totalEnd;
		}

		public string FormatCurrency(decimal amount)
		{
			var currency = _configService?.PrimaryCurrency?.Symbol ?? "EUR";
			var displaySymbol = currency switch
			{
				"EUR" => "€",
				"USD" => "$",
				"GBP" => "£",
				_ => currency + " "
			};
            return $"{displaySymbol}{amount.ToString("N2", CultureInfo.InvariantCulture)}";
		}

		public bool HasValues(TaxAccountDisplayModel account)
		{
			if (account.StartValue != 0 || account.EndValue != 0 || account.StartCashBalance != 0 || account.EndCashBalance != 0)
				return true;
			if ((account.StartHoldings != null && account.StartHoldings.Any()) ||
				(account.EndHoldings != null && account.EndHoldings.Any()) ||
				(account.Holdings != null && account.Holdings.Any()))
				return true;
			if (account.Transactions != null && account.Transactions.Any(tx => tx.Type != "KnownBalance" && tx.Amount != 0))
				return true;
			if (account.Dividends != null && account.Dividends.Any())
				return true;
			if (account.RealizedGainsLosses != null && account.RealizedGainsLosses.Any())
				return true;
			return false;
		}
	}
}
