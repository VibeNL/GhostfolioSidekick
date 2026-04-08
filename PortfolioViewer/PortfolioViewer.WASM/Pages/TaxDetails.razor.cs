using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
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
    [Inject] protected TaxDetailsService TaxService { get; set; } = default!;
    [Inject] protected NavigationManager? Navigation { get; set; } = default!;

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
    protected List<TaxAccountDisplayModel>? Accounts;

    protected override async Task OnInitializedAsync()
    {
        Years = await TaxService.GetAvailableYearsAsync();
        SelectedYear = Years.LastOrDefault(DateTime.Now.Year);
    }

    protected async Task ReloadAccounts()
    {
        Accounts = await TaxService.GetTaxAccountDetailsAsync(SelectedYear);
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
        return TaxService.FormatCurrency(amount);
    }

    protected bool HasValues(TaxAccountDisplayModel account)
    {
        return TaxService.HasValues(account);
    }
}
}
