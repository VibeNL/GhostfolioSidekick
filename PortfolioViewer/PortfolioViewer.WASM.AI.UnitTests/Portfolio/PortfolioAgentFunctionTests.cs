using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Portfolio;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.Portfolio
{
	public class PortfolioAgentFunctionTests
	{
		private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
		private readonly Mock<IServiceScope> _scopeMock;
		private readonly Mock<IServiceProvider> _serviceProviderMock;
		private readonly Mock<IHoldingsDataService> _holdingsServiceMock;
		private readonly Mock<IDividendsService> _dividendsServiceMock;
		private readonly PortfolioAgentFunction _sut;

		public PortfolioAgentFunctionTests()
		{
			_scopeFactoryMock = new Mock<IServiceScopeFactory>();
			_scopeMock = new Mock<IServiceScope>();
			_serviceProviderMock = new Mock<IServiceProvider>();
			_holdingsServiceMock = new Mock<IHoldingsDataService>();
			_dividendsServiceMock = new Mock<IDividendsService>();

			_serviceProviderMock
				.Setup(sp => sp.GetService(typeof(IHoldingsDataService)))
				.Returns(_holdingsServiceMock.Object);
			_serviceProviderMock
				.Setup(sp => sp.GetService(typeof(IDividendsService)))
				.Returns(_dividendsServiceMock.Object);

			_scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

			// IServiceScopeFactory.CreateAsyncScope() is an extension method that uses CreateScope()
			_scopeFactoryMock
				.Setup(f => f.CreateScope())
				.Returns(_scopeMock.Object);

			_sut = new PortfolioAgentFunction(_scopeFactoryMock.Object);
		}

		private static HoldingDisplayModel MakeHolding(string name, string symbol, decimal value, decimal gainLossPct = 10m, decimal weight = 5m)
		{
			var currency = Currency.USD;
			return new HoldingDisplayModel
			{
				Name = name,
				Symbols = [symbol],
				CurrentValue = new Money(currency, value),
				AveragePrice = new Money(currency, 100m),
				CurrentPrice = new Money(currency, 110m),
				GainLoss = new Money(currency, value * gainLossPct / 100m),
				GainLossPercentage = gainLossPct,
				Quantity = 1m,
				Weight = weight,
				Sector = "Technology",
				Currency = "USD",
			};
		}

		// ── GetHoldings ────────────────────────────────────────────────────────────

		[Fact]
		public async Task GetHoldings_WhenNoHoldings_ReturnsNoHoldingsMessage()
		{
			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var result = await _sut.GetHoldings();

			result.Should().Be("No holdings found in the portfolio.");
		}

		[Fact]
		public async Task GetHoldings_WithoutFilter_ReturnsTopHoldingsByValue()
		{
			var holdings = Enumerable.Range(1, 15)
				.Select(i => MakeHolding($"Company {i}", $"SYM{i}", i * 100m))
				.ToList();

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings();

			result.Should().Contain("Top 10 holdings by value");
			result.Should().Contain("Company 15"); // highest value
			result.Should().Contain("showing top 10 of 15");
		}

		[Fact]
		public async Task GetHoldings_WithSymbolFilter_ReturnsMatchingHoldings()
		{
			var holdings = new List<HoldingDisplayModel>
			{
				MakeHolding("Apple Inc", "AAPL", 5000m),
				MakeHolding("Microsoft", "MSFT", 4000m),
			};

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings(symbolFilter: "AAPL");

			result.Should().Contain("Apple Inc");
			result.Should().NotContain("Microsoft");
		}

		[Fact]
		public async Task GetHoldings_WithSymbolFilterNoMatch_ReturnsNotFoundMessage()
		{
			var holdings = new List<HoldingDisplayModel>
			{
				MakeHolding("Apple Inc", "AAPL", 5000m),
			};

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings(symbolFilter: "TSLA");

			result.Should().Be("No holdings matching 'TSLA' found.");
		}

		[Fact]
		public async Task GetHoldings_WithNameFilter_ReturnsMatchingHoldings()
		{
			var holdings = new List<HoldingDisplayModel>
			{
				MakeHolding("Apple Inc", "AAPL", 5000m),
				MakeHolding("Microsoft", "MSFT", 4000m),
			};

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings(symbolFilter: "apple");

			result.Should().Contain("Apple Inc");
			result.Should().NotContain("Microsoft");
		}

		[Fact]
		public async Task GetHoldings_CountClampsToMax20()
		{
			var holdings = Enumerable.Range(1, 25)
				.Select(i => MakeHolding($"Company {i}", $"SYM{i}", i * 100m))
				.ToList();

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings(count: 100);

			result.Should().Contain("Top 20 holdings by value");
		}

		[Fact]
		public async Task GetHoldings_WhenCountExactlyMatchesTotal_NoSuffixShown()
		{
			var holdings = Enumerable.Range(1, 3)
				.Select(i => MakeHolding($"Company {i}", $"SYM{i}", i * 100m))
				.ToList();

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetHoldings(count: 10);

			result.Should().NotContain("showing top");
		}

		// ── GetPortfolioSummary ────────────────────────────────────────────────────

		[Fact]
		public async Task GetPortfolioSummary_WhenNoHoldings_ReturnsNoDataMessage()
		{
			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var result = await _sut.GetPortfolioSummary();

			result.Should().Be("No portfolio data available.");
		}

		[Fact]
		public async Task GetPortfolioSummary_WithHoldings_ReturnsSummary()
		{
			var holdings = new List<HoldingDisplayModel>
			{
				MakeHolding("Apple Inc", "AAPL", 5000m, 15m),
				MakeHolding("Microsoft", "MSFT", 3000m, -5m),
				MakeHolding("Google", "GOOGL", 4000m, 10m),
				MakeHolding("Amazon", "AMZN", 2000m, 20m),
				MakeHolding("Tesla", "TSLA", 1000m, -10m),
				MakeHolding("Meta", "META", 1500m, 5m),
			};

			_holdingsServiceMock
				.Setup(s => s.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(holdings);

			var result = await _sut.GetPortfolioSummary();

			result.Should().Contain("Portfolio Summary:");
			result.Should().Contain("Positions");
			result.Should().Contain("Total value");
			result.Should().Contain("Top 5 winners");
			result.Should().Contain("Top 5 losers");
		}

		// ── GetUpcomingDividends ───────────────────────────────────────────────────

		[Fact]
		public async Task GetUpcomingDividends_WhenNone_ReturnsNoDataMessage()
		{
			_dividendsServiceMock
				.Setup(s => s.GetDividendsAsync())
				.ReturnsAsync([]);

			var result = await _sut.GetUpcomingDividends();

			result.Should().Be("No upcoming dividends found.");
		}

		[Fact]
		public async Task GetUpcomingDividends_WithDividends_ReturnsFormattedList()
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var dividends = new List<DividendModel>
			{
				new()
				{
					CompanyName = "Apple Inc",
					ExDate = today.AddDays(5),
					PaymentDate = today.AddDays(20),
					Amount = 0.25m,
					Currency = "USD",
					Quantity = 100,
					IsPredicted = false,
				},
				new()
				{
					CompanyName = "Microsoft",
					ExDate = today.AddDays(10),
					PaymentDate = today.AddDays(25),
					Amount = 0.75m,
					AmountPrimaryCurrency = 0.75m,
					PrimaryCurrency = "USD",
					Currency = "USD",
					Quantity = 50,
					IsPredicted = true,
				},
			};

			_dividendsServiceMock
				.Setup(s => s.GetDividendsAsync())
				.ReturnsAsync(dividends);

			var result = await _sut.GetUpcomingDividends();

			result.Should().Contain("Apple Inc");
			result.Should().Contain("Microsoft");
			result.Should().Contain("* = predicted");
		}

		// ── GetPortfolioPerformance ────────────────────────────────────────────────

		[Fact]
		public async Task GetPortfolioPerformance_WhenNoHistory_ReturnsNoDataMessage()
		{
			_holdingsServiceMock
				.Setup(s => s.GetPortfolioValueHistoryAsync(
					It.IsAny<DateOnly>(),
					It.IsAny<DateOnly>(),
					It.IsAny<int?>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var result = await _sut.GetPortfolioPerformance("2024-01-01", "2024-12-31");

			result.Should().Contain("No portfolio history found between");
		}

		[Fact]
		public async Task GetPortfolioPerformance_WithHistory_ReturnsFormattedPerformance()
		{
			var history = new List<PortfolioValueHistoryPoint>
			{
				new() { Date = new DateOnly(2024, 1, 1), Value = 10000m, Invested = 9000m },
				new() { Date = new DateOnly(2024, 4, 1), Value = 10500m, Invested = 9000m },
				new() { Date = new DateOnly(2024, 7, 1), Value = 11000m, Invested = 9500m },
				new() { Date = new DateOnly(2024, 12, 31), Value = 12000m, Invested = 10000m },
			};

			_holdingsServiceMock
				.Setup(s => s.GetPortfolioValueHistoryAsync(
					It.IsAny<DateOnly>(),
					It.IsAny<DateOnly>(),
					It.IsAny<int?>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(history);

			var result = await _sut.GetPortfolioPerformance("2024-01-01", "2024-12-31");

			result.Should().Contain("Performance");
			result.Should().Contain("Start");
			result.Should().Contain("End");
			result.Should().Contain("Change");
			result.Should().Contain("Quarterly snapshots");
		}

		[Fact]
		public async Task GetPortfolioPerformance_WithNullDates_UsesDefaults()
		{
			var history = new List<PortfolioValueHistoryPoint>
			{
				new() { Date = new DateOnly(2024, 1, 1), Value = 10000m, Invested = 9000m },
				new() { Date = new DateOnly(2024, 12, 31), Value = 12000m, Invested = 10000m },
			};

			_holdingsServiceMock
				.Setup(s => s.GetPortfolioValueHistoryAsync(
					It.IsAny<DateOnly>(),
					It.IsAny<DateOnly>(),
					It.IsAny<int?>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(history);

			// No date arguments → should use defaults without throwing
			var result = await _sut.GetPortfolioPerformance();

			result.Should().Contain("Performance");
		}

		[Fact]
		public async Task GetPortfolioPerformance_WithInvalidDateStrings_UsesDefaults()
		{
			var history = new List<PortfolioValueHistoryPoint>
			{
				new() { Date = new DateOnly(2024, 1, 1), Value = 10000m, Invested = 9000m },
				new() { Date = new DateOnly(2024, 12, 31), Value = 12000m, Invested = 10000m },
			};

			_holdingsServiceMock
				.Setup(s => s.GetPortfolioValueHistoryAsync(
					It.IsAny<DateOnly>(),
					It.IsAny<DateOnly>(),
					It.IsAny<int?>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(history);

			// Invalid format → ParseDateOrDefault should return fallback
			var result = await _sut.GetPortfolioPerformance("not-a-date", "also-not-a-date");

			result.Should().Contain("Performance");
		}
	}
}
