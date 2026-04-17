using GhostfolioSidekick.Model;
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

        protected record YearComparisonRow(
            string AccountName,
            TaxReportRow? Start,
            TaxReportRow? End);

        protected IEnumerable<YearComparisonRow> GetComparisonRows(int year)
        {
            var dates = GetDatesForYear(year).ToList();
            var startDate = dates.FirstOrDefault();
            var endDate = dates.Count > 1 ? dates.Last() : (DateOnly?)null;

            var startRows = GetRowsForDate(year, startDate).ToDictionary(r => r.AccountName);
            var endRows = endDate.HasValue
                ? GetRowsForDate(year, endDate.Value).ToDictionary(r => r.AccountName)
                : [];

            var accounts = startRows.Keys.Union(endRows.Keys).OrderBy(n => n);
            return accounts.Select(name => new YearComparisonRow(
                name,
                startRows.GetValueOrDefault(name),
                endRows.GetValueOrDefault(name)));
        }

        protected YearComparisonRow GetComparisonTotal(int year)
        {
            var rows = GetComparisonRows(year).ToList();
            var currency = ReportData.First(r => r.Year == year).TotalValue.Currency;

            TaxReportRow? Sum(Func<YearComparisonRow, TaxReportRow?> selector)
            {
                var items = rows.Select(selector).Where(r => r != null).ToList();
                if (items.Count == 0)
                    return null;
                return new TaxReportRow
                {
                    Year = year,
                    Date = default,
                    AccountId = 0,
                    AccountName = "Total",
                    AssetValue = new Money(currency, items.Sum(r => r!.AssetValue.Amount)),
                    CashBalance = new Money(currency, items.Sum(r => r!.CashBalance.Amount)),
                    TotalValue = new Money(currency, items.Sum(r => r!.TotalValue.Amount))
                };
            }

            return new YearComparisonRow("Total", Sum(r => r.Start), Sum(r => r.End));
        }

        protected static string ChangeClass(decimal change) =>
            change > 0 ? "text-success" : change < 0 ? "text-danger" : "text-muted";

        protected static string ChangeIcon(decimal change) =>
            change > 0 ? "bi-arrow-up-circle-fill" : change < 0 ? "bi-arrow-down-circle-fill" : "bi-dash-circle";

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

