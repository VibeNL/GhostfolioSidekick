using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Services;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Demo application to showcase portfolio performance calculation capabilities
	/// </summary>
	public class PortfolioPerformanceDemo
	{
		/// <summary>
		/// Run a demonstration of portfolio performance calculations
		/// </summary>
		public static void RunDemo()
		{
			Console.WriteLine("=== Portfolio Performance Calculator Demo ===");
			Console.WriteLine();

			// Create sample data
			var holdings = CreateSamplePortfolioData();
			var calculator = new PortfolioPerformanceCalculator();

			// Calculate performance for different periods
			var periods = new[]
			{
				("Last Quarter", DateTime.Now.AddMonths(-3), DateTime.Now),
				("Last 6 Months", DateTime.Now.AddMonths(-6), DateTime.Now),
				("Last Year", DateTime.Now.AddYears(-1), DateTime.Now)
			};

			foreach (var (periodName, startDate, endDate) in periods)
			{
				Console.WriteLine($"=== {periodName} Performance Analysis ===");
				AnalyzePeriodPerformance(calculator, holdings, startDate, endDate, Currency.EUR);
				Console.WriteLine();
			}

			// Show TWR calculation periods
			DemonstrateTWRCalculation(calculator, holdings);
		}

		private static void AnalyzePeriodPerformance(
			PortfolioPerformanceCalculator calculator,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			try
			{
				var allActivities = holdings
					.SelectMany(h => h.Activities)
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.ToList();

				if (!allActivities.Any())
				{
					Console.WriteLine("No activities found for this period.");
					return;
				}

				var performance = calculator.CalculateBasicPerformance(
					allActivities,
					holdings,
					startDate,
					endDate,
					baseCurrency);

				// Display results
				Console.WriteLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
				Console.WriteLine($"Base Currency: {baseCurrency.Symbol}");
				Console.WriteLine($"Time-Weighted Return: {performance.TimeWeightedReturn:F2}%");
				Console.WriteLine($"Total Dividends: {performance.TotalDividends.Amount:F2} {performance.TotalDividends.Currency.Symbol}");
				Console.WriteLine($"Dividend Yield: {performance.DividendYield:F2}%");
				Console.WriteLine($"Currency Impact: {performance.CurrencyImpact:F2}%");
				Console.WriteLine($"Net Cash Flows: {performance.NetCashFlows.Amount:F2} {performance.NetCashFlows.Currency.Symbol}");
				
				// Calculate additional metrics
				var totalReturn = performance.FinalValue.Amount - performance.InitialValue.Amount - performance.NetCashFlows.Amount;
				Console.WriteLine($"Absolute Return: {totalReturn:F2} {baseCurrency.Symbol}");
				
				var days = (endDate - startDate).TotalDays;
				var years = days / 365.25;
				if (years > 0)
				{
					var annualizedReturn = (Math.Pow((double)(1 + performance.TimeWeightedReturn / 100), 1 / years) - 1) * 100;
					Console.WriteLine($"Annualized Return: {annualizedReturn:F2}%");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error calculating performance: {ex.Message}");
			}
		}

		private static void DemonstrateTWRCalculation(
			PortfolioPerformanceCalculator calculator,
			List<Holding> holdings)
		{
			Console.WriteLine("=== Time-Weighted Return (TWR) Calculation Demo ===");

			var startDate = DateTime.Now.AddMonths(-6);
			var endDate = DateTime.Now;
			
			var allActivities = holdings
				.SelectMany(h => h.Activities)
				.Where(a => a.Date >= startDate && a.Date <= endDate)
				.ToList();

			if (!allActivities.Any())
			{
				Console.WriteLine("No activities found for TWR demonstration.");
				return;
			}

			// Show how periods are created for TWR calculation
			var periods = calculator.CreatePeriodsForTWR(allActivities, startDate, endDate);
			
			Console.WriteLine($"TWR Calculation Periods ({periods.Count} periods):");
			
			for (int i = 0; i < periods.Count; i++)
			{
				var period = periods[i];
				Console.WriteLine($"Period {i + 1}: {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}");
				Console.WriteLine($"  Start Value: {period.StartValue.Amount:F2} {period.StartValue.Currency.Symbol}");
				Console.WriteLine($"  End Value: {period.EndValue.Amount:F2} {period.EndValue.Currency.Symbol}");
				Console.WriteLine($"  Cash Flow: {period.CashFlow.Amount:F2} {period.CashFlow.Currency.Symbol}");
				Console.WriteLine($"  Period Return: {period.CalculatePeriodReturn():F4} ({period.CalculatePeriodReturn() * 100:F2}%)");
				Console.WriteLine($"  Activities: {period.Activities.Count}");
			}

			// Calculate compound return
			decimal cumulativeReturn = 1.0m;
			foreach (var period in periods)
			{
				cumulativeReturn *= (1 + period.CalculatePeriodReturn());
			}
			
			var totalTWR = (cumulativeReturn - 1) * 100;
			Console.WriteLine($"Total Time-Weighted Return: {totalTWR:F2}%");
		}

		private static List<Holding> CreateSamplePortfolioData()
		{
			Console.WriteLine("Creating sample portfolio data...");

			var account = new Account("Demo Account");
			var baseDate = DateTime.Now.AddYears(-1);

			// Create holdings with sample activities
			var holdings = new List<Holding>();

			// Holding 1: European ETF (EUR)
			var eurHolding = new Holding
			{
				SymbolProfiles = new List<SymbolProfile>
				{
					new SymbolProfile
					{
						Symbol = "VWCE.AS",
						Currency = Currency.EUR,
						DataSource = "YAHOO"
					}
				},
				Activities = new List<Model.Activities.Activity>
				{
					new BuySellActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("VWCE.AS") }, 
						baseDate.AddDays(30), 
						10, 
						new Money(Currency.EUR, 85.50m), 
						"1", 
						1, 
						"Initial buy"),
					new BuySellActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("VWCE.AS") }, 
						baseDate.AddDays(90), 
						5, 
						new Money(Currency.EUR, 87.20m), 
						"2", 
						1, 
						"Additional buy"),
					new DividendActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("VWCE.AS") }, 
						baseDate.AddDays(120), 
						new Money(Currency.EUR, 12.50m), 
						"3", 
						1, 
						"Quarterly dividend"),
					new CashDepositWithdrawalActivity(
						account, 
						null, 
						baseDate.AddDays(15), 
						new Money(Currency.EUR, 1000m), 
						"4", 
						1, 
						"Initial deposit")
				}
			};

			// Holding 2: US Stock (USD)
			var usdHolding = new Holding
			{
				SymbolProfiles = new List<SymbolProfile>
				{
					new SymbolProfile
					{
						Symbol = "AAPL",
						Currency = Currency.USD,
						DataSource = "YAHOO"
					}
				},
				Activities = new List<Model.Activities.Activity>
				{
					new BuySellActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("AAPL") }, 
						baseDate.AddDays(60), 
						20, 
						new Money(Currency.USD, 150.75m), 
						"5", 
						1, 
						"Tech stock buy"),
					new DividendActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("AAPL") }, 
						baseDate.AddDays(180), 
						new Money(Currency.USD, 24.20m), 
						"6", 
						1, 
						"Dividend payment"),
					new BuySellActivity(
						account, 
						null, 
						new List<PartialSymbolIdentifier> { PartialSymbolIdentifier.CreateStockAndETF("AAPL") }, 
						baseDate.AddDays(200), 
						-5, 
						new Money(Currency.USD, 165.30m), 
						"7", 
						1, 
						"Partial sell")
				}
			};

			holdings.Add(eurHolding);
			holdings.Add(usdHolding);

			Console.WriteLine($"Created {holdings.Count} holdings with {holdings.SelectMany(h => h.Activities).Count()} total activities");
			return holdings;
		}
	}
}