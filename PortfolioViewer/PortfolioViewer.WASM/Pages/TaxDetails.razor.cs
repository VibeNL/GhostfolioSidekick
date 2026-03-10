using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public class TaxDetailsBase : ComponentBase
	{
		[Inject] protected IAccountDataService? AccountService { get; set; } = default!;
		[Inject] protected ITransactionService? TransactionService { get; set; } = default!;
		[Inject] protected NavigationManager? Navigation { get; set; } = default!;
		[Inject] protected IServerConfigurationService? ConfigService { get; set; } = default!;

		protected List<int> Years = new();
        private int _selectedYear;
        protected int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    _ = ReloadAccounts();
                }
            }
        }
		protected List<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TaxAccountDisplayModel>? Accounts;

        protected override async Task OnInitializedAsync()
        {
            Years = await TransactionService.GetAvailableYearsAsync();
            SelectedYear = Years.LastOrDefault(DateTime.Now.Year);
            await ReloadAccounts();
        }

        protected async Task ReloadAccounts()
        {
            Accounts = await AccountService.GetTaxAccountDetailsAsync(SelectedYear);
            if (Accounts != null)
            {
                foreach (var account in Accounts)
                {
                    ComputeSymbolRows(account);
                }
            }
            StateHasChanged();
        }

        protected async Task OnYearChanged(ChangeEventArgs e)
        {
            if (e.Value is string s && int.TryParse(s, out var year))
            {
                SelectedYear = year;
                await ReloadAccounts();
            }
        }

		protected string FormatCurrency(decimal amount)
		{
			// Use primary currency from config
			var currency = ConfigService?.PrimaryCurrency?.Symbol ?? "EUR";

			// EUR -> €, USD -> $, GBP -> £
			var displaySymbol = currency switch
			{
				"EUR" => "€",
				"USD" => "$",
				"GBP" => "£",
				_ => currency + " "
			};

			// Format with symbol and two decimals
			return $"{displaySymbol}{amount:N2}";
		}

		protected bool HasValues(TaxAccountDisplayModel account)
		{
			// Check numeric values
			if (account.StartValue != 0 || account.EndValue != 0 || account.StartCashBalance != 0 || account.EndCashBalance != 0)
				return true;

			// Check holdings
			if ((account.StartHoldings != null && account.StartHoldings.Any()) ||
				(account.EndHoldings != null && account.EndHoldings.Any()) ||
				(account.Holdings != null && account.Holdings.Any()))
				return true;

			// Check transactions
			if (account.Transactions != null && account.Transactions.Any(tx => tx.Type != "KnownBalance" && tx.Amount != 0))
				return true;

			// Check dividends
			if (account.Dividends != null && account.Dividends.Any())
				return true;

			// Check realized gains/losses
			if (account.RealizedGainsLosses != null && account.RealizedGainsLosses.Any())
				return true;

			return false;
		}
        protected void ComputeSymbolRows(GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TaxAccountDisplayModel account)
        {
            var startSymbols = account.StartHoldings?.Select(h => h.Symbol).ToHashSet() ?? new HashSet<string?>();
            var endSymbols = account.EndHoldings?.Select(h => h.Symbol).ToHashSet() ?? new HashSet<string?>();
            var allSymbols = startSymbols.Union(endSymbols).OrderBy(s => s).ToList();
            var rows = new List<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TaxAccountDisplayModel.SymbolRow>();
            decimal totalStart = 0m;
            decimal totalEnd = 0m;
            foreach (var symbol in allSymbols)
            {
                var start = account.StartHoldings?.FirstOrDefault(h => h.Symbol == symbol);
                var end = account.EndHoldings?.FirstOrDefault(h => h.Symbol == symbol);
                rows.Add(new GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TaxAccountDisplayModel.SymbolRow
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
	}
}
