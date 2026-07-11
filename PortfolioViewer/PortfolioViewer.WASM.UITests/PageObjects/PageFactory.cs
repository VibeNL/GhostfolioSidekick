using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects;

/// <summary>
/// Factory for creating page objects with a shared IPage instance.
/// Eliminates boilerplate in test classes.
/// </summary>
public static class PageFactory
{
	public static LoginPage CreateLoginPage(IPage page) => new(page);
	public static HomePage CreateHomePage(IPage page) => new(page);
	public static HoldingsPage CreateHoldingsPage(IPage page) => new(page);
	public static AccountsPage CreateAccountsPage(IPage page) => new(page);
	public static TaxReportPage CreateTaxReportPage(IPage page) => new(page);
	public static TopMoversPage CreateTopMoversPage(IPage page) => new(page);
	public static PortfolioTimeSeriesPage CreatePortfolioTimeSeriesPage(IPage page) => new(page);
	public static UpcomingDividendsPage CreateUpcomingDividendsPage(IPage page) => new(page);
	public static TaskStatusPage CreateTaskStatusPage(IPage page) => new(page);
	public static TablesPage CreateTablesPage(IPage page) => new(page);
	public static DataIssuesPage CreateDataIssuesPage(IPage page) => new(page);
	public static TransactionsPage CreateTransactionsPage(IPage page) => new(page);
	public static PriceTargetsPage CreatePriceTargetsPage(IPage page) => new(page);
}
