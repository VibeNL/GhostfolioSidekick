using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Services;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Demo application showcasing market data-driven portfolio performance calculations
	/// </summary>
	public class MarketDataPortfolioPerformanceDemo
	{
		/// <summary>
		/// Run a demonstration of market data-driven portfolio performance calculations
		/// </summary>
		public static void RunDemo()
		{
			Console.WriteLine("=== Market Data-Driven Portfolio Performance Calculator Demo ===");
			Console.WriteLine();

			// Create sample data with market data
			var holdings = CreateSamplePortfolioDataWithMarketData();
			var calculator = new PortfolioPerformanceCalculator();

			// Show market data coverage
			DisplayMarketDataCoverage(holdings);

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
				AnalyzePeriodPerformanceWithMarketData(calculator, holdings, startDate, endDate, Currency.EUR);
				Console.WriteLine();
			}

			// Demonstrate accurate TWR calculation with market data
			DemonstrateAccurateTWRCalculation(holdings);

			// Show portfolio valuation using market data
			DemonstratePortfolioValuation(holdings, Currency.EUR);
		}

		private static void DisplayMarketDataCoverage(List<Holding> holdings)
		{
			Console.WriteLine("=== Market Data Coverage Assessment ===");

			var totalHoldings = holdings.Count;
			var holdingsWithMarketData = 0;
			var totalMarketDataPoints = 0;

			foreach (var holding in holdings)
			{
				var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
				if (symbolProfile == null) continue;

				if (symbolProfile.MarketData != null && symbolProfile.MarketData.Any())
				{
					holdingsWithMarketData++;
					totalMarketDataPoints += symbolProfile.MarketData.Count;

					var latestData = symbolProfile.MarketData.OrderByDescending(md => md.Date).First();
					var earliestData = symbolProfile.MarketData.OrderBy(md => md.Date).First();

					Console.WriteLine($"Symbol: {symbolProfile.Symbol}");
					Console.WriteLine($"  Market Data Points: {symbolProfile.MarketData.Count}");
					Console.WriteLine($"  Date Range: {earliestData.Date} to {latestData.Date}");
					Console.WriteLine($"  Latest Price: {latestData.Close.Amount:F2} {latestData.Close.Currency.Symbol}");
				}
				else
				{
					Console.WriteLine($"Symbol: {symbolProfile.Symbol} - No market data available");
				}
			}

			var coverage = (double)holdingsWithMarketData / totalHoldings * 100;
			Console.WriteLine($"\nMarket Data Coverage: {coverage:F1}% ({holdingsWithMarketData}/{totalHoldings} holdings)");
			Console.WriteLine($"Total Market Data Points: {totalMarketDataPoints}");
			Console.WriteLine();
		}

		private static void AnalyzePeriodPerformanceWithMarketData(
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

				// Calculate accurate portfolio values using market data
				var initialValue = CalculateAccuratePortfolioValue(holdings, startDate, baseCurrency);
				var finalValue = CalculateAccuratePortfolioValue(holdings, endDate, baseCurrency);

				// Calculate basic performance (enhanced with market data values)
				var performance = calculator.CalculateBasicPerformance(
					allActivities,
					holdings,
					startDate,
					endDate,
					baseCurrency);

				// Override with market data-driven values
				var marketDataPerformance = new Model.Portfolio.PortfolioPerformance(
					performance.TimeWeightedReturn,
					performance.TotalDividends,
					performance.DividendYield,
					performance.CurrencyImpact,
					startDate,
					endDate,
					baseCurrency,
					initialValue,
					finalValue,
					performance.NetCashFlows);

				// Display results
				Console.WriteLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
				Console.WriteLine($"Base Currency: {baseCurrency.Symbol}");
				Console.WriteLine($"Initial Portfolio Value (Market Data): {initialValue.Amount:F2} {initialValue.Currency.Symbol}");
				Console.WriteLine($"Final Portfolio Value (Market Data): {finalValue.Amount:F2} {finalValue.Currency.Symbol}");
				Console.WriteLine($"Time-Weighted Return: {marketDataPerformance.TimeWeightedReturn:F2}%");
				Console.WriteLine($"Total Dividends: {marketDataPerformance.TotalDividends.Amount:F2} {marketDataPerformance.TotalDividends.Currency.Symbol}");
				Console.WriteLine($"Dividend Yield: {marketDataPerformance.DividendYield:F2}%");
				Console.WriteLine($"Currency Impact: {marketDataPerformance.CurrencyImpact:F2}%");
				
				// Calculate additional metrics
				var totalReturn = finalValue.Amount - initialValue.Amount - performance.NetCashFlows.Amount;
				var totalReturnPercentage = initialValue.Amount != 0 ? (totalReturn / initialValue.Amount) * 100 : 0;
				
				Console.WriteLine($"Absolute Return (Market Data): {totalReturn:F2} {baseCurrency.Symbol}");
				Console.WriteLine($"Total Return % (Market Data): {totalReturnPercentage:F2}%");
				
				var days = (endDate - startDate).TotalDays;
				var years = days / 365.25;
				if (years > 0)
				{
					var annualizedReturn = (Math.Pow((double)(1 + totalReturnPercentage / 100), 1 / years) - 1) * 100;
					Console.WriteLine($"Annualized Return (Market Data): {annualizedReturn:F2}%");
				}

				Console.WriteLine("Analysis Quality: Market Data-Driven (Most Accurate)");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error calculating performance with market data: {ex.Message}");
			}
		}

		private static Money CalculateAccuratePortfolioValue(List<Holding> holdings, DateTime date, Currency baseCurrency)
		{
			var totalValue = new Money(baseCurrency, 0);

			foreach (var holding in holdings)
			{
				var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
				if (symbolProfile == null) continue;

				// Calculate quantity at the given date
				var quantity = holding.Activities
					.Where(a => a.Date <= date)
					.OfType<BuySellActivity>()
					.Sum(a => a.Quantity);

				if (quantity <= 0) continue;

				// Get market price at the specified date
				var marketPrice = GetMarketPriceAtDate(symbolProfile, date);
				if (marketPrice == null) continue;

				// Calculate value in holding's currency
				var holdingValue = quantity * marketPrice.Amount;

				// Simple conversion (in a real implementation, use proper currency exchange)
				if (marketPrice.Currency.Symbol == baseCurrency.Symbol)
				{
					totalValue = totalValue.Add(new Money(baseCurrency, holdingValue));
				}
				else
				{
					// Placeholder conversion rate
					var conversionRate = GetSimpleConversionRate(marketPrice.Currency, baseCurrency);
					totalValue = totalValue.Add(new Money(baseCurrency, holdingValue * conversionRate));
				}
			}

			return totalValue;
		}

		private static Money? GetMarketPriceAtDate(SymbolProfile symbolProfile, DateTime date)
		{
			if (symbolProfile.MarketData == null || !symbolProfile.MarketData.Any())
			{
				return null;
			}

			var targetDate = DateOnly.FromDateTime(date);
			
			// Try to find exact date match first
			var exactMatch = symbolProfile.MarketData.FirstOrDefault(md => md.Date == targetDate);
			if (exactMatch != null)
			{
				return exactMatch.Close;
			}

			// Find closest date before the target date
			var closestBefore = symbolProfile.MarketData
				.Where(md => md.Date <= targetDate)
				.OrderByDescending(md => md.Date)
				.FirstOrDefault();

			if (closestBefore != null)
			{
				return closestBefore.Close;
			}

			// If no data before target date, use earliest available data
			var earliest = symbolProfile.MarketData
				.OrderBy(md => md.Date)
				.FirstOrDefault();

			return earliest?.Close;
		}

		private static decimal GetSimpleConversionRate(Currency from, Currency to)
		{
			// Simplified conversion rates for demo purposes
			if (from.Symbol == to.Symbol) return 1.0m;
			
			// USD to EUR
			if (from.Symbol == "USD" && to.Symbol == "EUR") return 0.85m;
			if (from.Symbol == "EUR" && to.Symbol == "USD") return 1.18m;
			
			// Default fallback
			return 1.0m;
		}

		private static void DemonstrateAccurateTWRCalculation(List<Holding> holdings)
		{
			Console.WriteLine("=== Market Data-Driven TWR Calculation Demo ===");

			var startDate = DateTime.Now.AddMonths(-6);
			var endDate = DateTime.Now;
			var baseCurrency = Currency.EUR;

			Console.WriteLine($"Calculating accurate TWR from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
			Console.WriteLine("Using market data for precise portfolio valuations...");
			Console.WriteLine();

			// Show portfolio value progression using market data
			var valuationDates = new[]
			{
				startDate,
				startDate.AddMonths(1),
				startDate.AddMonths(2),
				startDate.AddMonths(3),
				startDate.AddMonths(4),
				startDate.AddMonths(5),
				endDate
			};

			Console.WriteLine("Portfolio Value Progression (Market Data):");
			foreach (var date in valuationDates)
			{
				var portfolioValue = CalculateAccuratePortfolioValue(holdings, date, baseCurrency);
				Console.WriteLine($"  {date:yyyy-MM-dd}: {portfolioValue.Amount:F2} {portfolioValue.Currency.Symbol}");
			}

			Console.WriteLine();
			Console.WriteLine("This accurate valuation enables precise TWR calculation that eliminates");
			Console.WriteLine("the impact of cash flow timing on performance measurement.");
		}

		private static void DemonstratePortfolioValuation(List<Holding> holdings, Currency baseCurrency)
		{
			Console.WriteLine("=== Current Portfolio Valuation (Market Data) ===");

			var currentDate = DateTime.Now;
			var totalValue = new Money(baseCurrency, 0);

			Console.WriteLine("Individual Holdings:");
			foreach (var holding in holdings)
			{
				var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
				if (symbolProfile == null) continue;

				var quantity = holding.Activities.OfType<BuySellActivity>().Sum(a => a.Quantity);
				if (quantity <= 0) continue;

				var marketPrice = GetMarketPriceAtDate(symbolProfile, currentDate);
				if (marketPrice == null) continue;

				var holdingValue = quantity * marketPrice.Amount;
				var conversionRate = GetSimpleConversionRate(marketPrice.Currency, baseCurrency);
				var convertedValue = holdingValue * conversionRate;

				totalValue = totalValue.Add(new Money(baseCurrency, convertedValue));

				Console.WriteLine($"  {symbolProfile.Symbol}:");
				Console.WriteLine($"    Quantity: {quantity:F4}");
				Console.WriteLine($"    Market Price: {marketPrice.Amount:F2} {marketPrice.Currency.Symbol}");
				Console.WriteLine($"    Value: {convertedValue:F2} {baseCurrency.Symbol}");
			}

			Console.WriteLine();
			Console.WriteLine($"Total Portfolio Value: {totalValue.Amount:F2} {totalValue.Currency.Symbol}");
			Console.WriteLine("Valuation Method: Market Data-Driven (Real-time accurate pricing)");
		}

		private static List<Holding> CreateSamplePortfolioDataWithMarketData()
		{
			Console.WriteLine("Creating sample portfolio data with market data...");

			var account = new Account("Demo Account");
			var baseDate = DateTime.Now.AddYears(-1);

			// Create holdings with sample activities and market data
			var holdings = new List<Holding>();

			// Holding 1: European ETF (EUR) with market data
			var eurEtfMarketData = GenerateMarketData(Currency.EUR, 85.50m, baseDate.AddDays(-365), DateTime.Now);
			var eurHolding = new Holding
			{
				SymbolProfiles = new List<SymbolProfile>
				{
					new SymbolProfile
					{
						Symbol = "VWCE.AS",
						Currency = Currency.EUR,
						DataSource = "YAHOO",
						MarketData = eurEtfMarketData
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

			// Holding 2: US Stock (USD) with market data
			var usdStockMarketData = GenerateMarketData(Currency.USD, 150.75m, baseDate.AddDays(-365), DateTime.Now);
			var usdHolding = new Holding
			{
				SymbolProfiles = new List<SymbolProfile>
				{
					new SymbolProfile
					{
						Symbol = "AAPL",
						Currency = Currency.USD,
						DataSource = "YAHOO",
						MarketData = usdStockMarketData
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

			Console.WriteLine($"Created {holdings.Count} holdings with market data");
			Console.WriteLine($"Total activities: {holdings.SelectMany(h => h.Activities).Count()}");
			Console.WriteLine($"Total market data points: {holdings.SelectMany(h => h.SymbolProfiles).SelectMany(sp => sp.MarketData ?? new List<MarketData>()).Count()}");
			
			return holdings;
		}

		private static List<MarketData> GenerateMarketData(Currency currency, decimal startPrice, DateTime startDate, DateTime endDate)
		{
			var marketData = new List<MarketData>();
			var random = new Random(42); // Fixed seed for consistent demo data
			var currentPrice = startPrice;
			var currentDate = startDate;

			while (currentDate <= endDate)
			{
				// Generate daily price movement (simulate market volatility)
				var changePercent = (decimal)(random.NextDouble() - 0.5) * 0.04m; // ±2% daily change
				currentPrice *= (1 + changePercent);

				var open = currentPrice * (1 + (decimal)(random.NextDouble() - 0.5) * 0.01m);
				var high = Math.Max(open, currentPrice) * (1 + (decimal)random.NextDouble() * 0.02m);
				var low = Math.Min(open, currentPrice) * (1 - (decimal)random.NextDouble() * 0.02m);
				var volume = 100000 + random.Next(50000);

				marketData.Add(new MarketData(
					new Money(currency, Math.Round(currentPrice, 2)),
					new Money(currency, Math.Round(open, 2)),
					new Money(currency, Math.Round(high, 2)),
					new Money(currency, Math.Round(low, 2)),
					volume,
					DateOnly.FromDateTime(currentDate)));

				currentDate = currentDate.AddDays(1);

				// Skip weekends for stock market data
				if (currentDate.DayOfWeek == DayOfWeek.Saturday)
					currentDate = currentDate.AddDays(2);
			}

			return marketData;
		}
	}
}