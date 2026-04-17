using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class TaxReport : ComponentBase
    {
        [Inject]
        private IAccountDataService AccountDataService { get; set; } = default!;

        [Inject]
        private IPrivacyModeService PrivacyModeService { get; set; } = default!;

        protected bool IsLoading { get; set; }
        protected bool HasError { get; set; }
        protected string ErrorMessage { get; set; } = string.Empty;
        protected List<TaxReportRow> ReportData { get; set; } = [];

        protected int? SelectedYear { get; set; }

        protected IEnumerable<int> Years => ReportData.Select(r => r.Year).Distinct().OrderBy(y => y);

        protected IEnumerable<int> VisibleYears => SelectedYear.HasValue ? [SelectedYear.Value] : Years;

        protected void SelectYear(int? year) => SelectedYear = year;

        protected IEnumerable<DateOnly> GetDatesForYear(int year) =>
            ReportData.Where(r => r.Year == year).Select(r => r.Date).Distinct().Order();

        protected IEnumerable<TaxReportRow> GetRowsForDate(int year, DateOnly date) =>
            ReportData.Where(r => r.Year == year && r.Date == date).OrderBy(r => r.AccountName);

        protected TaxReportRow? GetTotalForDate(int year, DateOnly date)
        {
            var rows = ReportData.Where(r => r.Year == year && r.Date == date).ToList();
            if (rows.Count == 0)
                return null;

            var currency = rows[0].TotalValue.Currency;
            return new TaxReportRow
            {
                Year = year,
                Date = date,
                AccountId = 0,
                AccountName = "Total",
                AssetValue = new GhostfolioSidekick.Model.Money(currency, rows.Sum(r => r.AssetValue.Amount)),
                CashBalance = new GhostfolioSidekick.Model.Money(currency, rows.Sum(r => r.CashBalance.Amount)),
                TotalValue = new GhostfolioSidekick.Model.Money(currency, rows.Sum(r => r.TotalValue.Amount))
            };
        }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                ReportData = await AccountDataService.GetTaxReportAsync();
                SelectedYear = Years.Any() ? Years.Max() : null;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
