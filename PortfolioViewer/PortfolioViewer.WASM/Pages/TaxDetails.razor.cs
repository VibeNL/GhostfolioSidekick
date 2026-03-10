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
		protected int SelectedYear;
		protected List<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.TaxAccountDisplayModel>? Accounts;

		protected override async Task OnInitializedAsync()
		{
			Years = await TransactionService.GetAvailableYearsAsync();
			SelectedYear = Years.LastOrDefault(DateTime.Now.Year);
			await LoadAccounts();
		}

		protected async Task LoadAccounts()
		{
			Accounts = await AccountService.GetTaxAccountDetailsAsync(SelectedYear);
		}

		protected async Task OnYearChanged(ChangeEventArgs e)
		{
			if (e.Value is string s && int.TryParse(s, out var year))
			{
				SelectedYear = year;
				await LoadAccounts();
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
	}
}
