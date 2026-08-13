using AwesomeAssertions;
using GhostfolioSidekick.ExternalDataProvider.Citi;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer;

public class MarketDataAdrRatioTaskTests
{
	private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
	private readonly Mock<IAdrRatioProvider> _mockAdrRatioProvider;
	private readonly MarketDataAdrRatioTask _marketDataAdrRatioTask;

	public MarketDataAdrRatioTaskTests()
	{
		_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
		_mockAdrRatioProvider = new Mock<IAdrRatioProvider>();
		_marketDataAdrRatioTask = new MarketDataAdrRatioTask(
			_mockDbContextFactory.Object,
			_mockAdrRatioProvider.Object);
	}

	[Fact]
	public void Priority_ShouldReturnMarketDataAdrRatio()
	{
		// Act
		var priority = _marketDataAdrRatioTask.Priority;

		// Assert
		priority.Should().Be(TaskPriority.MarketDataAdrRatio);
	}

	[Fact]
	public void ExecutionFrequency_ShouldReturnHourly()
	{
		// Act
		var frequency = _marketDataAdrRatioTask.ExecutionFrequency;

		// Assert
		frequency.Should().Be(TimeSpan.FromHours(1));
	}

	[Fact]
	public void ExceptionsAreFatal_ShouldReturnFalse()
	{
		// Act
		var exceptionsAreFatal = _marketDataAdrRatioTask.ExceptionsAreFatal;

		// Assert
		exceptionsAreFatal.Should().BeFalse();
	}

	[Fact]
	public void Name_ShouldReturnCorrectName()
	{
		// Act
		var name = _marketDataAdrRatioTask.Name;

		// Assert
		name.Should().Be("Market Data ADR/GDR Ratio Gatherer");
	}

	[Fact]
	public async Task DoWork_ShouldSkipNonStockSymbols()
	{
		// Arrange
		var symbolProfiles = new List<SymbolProfile>
		{
			new() { Symbol = "BTC", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.CryptoCurrency, ISIN = "US1234567890" },
			new() { Symbol = "AAPL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock, ISIN = "US0378331005" }
		};

		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0378331005"))
			.ReturnsAsync((decimal?)25);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		// Only AAPL (stock) should be processed, BTC (crypto) should be skipped
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync("US1234567890"),
			Times.Never);
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync("US0378331005"),
			Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldSkipGhostfolioDataSources()
	{
		// Arrange
		var symbolProfiles = new List<SymbolProfile>
		{
			new() { Symbol = "AAPL", DataSource = "GHOSTFOLIO-YAHOO", AssetSubClass = AssetSubClass.Stock, ISIN = "US0378331005" },
			new() { Symbol = "GOOGL", DataSource = "YAHOO", AssetSubClass = AssetSubClass.Stock, ISIN = "US0231351067" }
		};

		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0231351067"))
			.ReturnsAsync((decimal?)1);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		// Only GOOGL should be processed, AAPL with GHOSTFOLIO datasource should be filtered out
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync("US0378331005"),
			Times.Never);
	}

	[Fact]
	public async Task DoWork_ShouldSkipSymbolsWithoutIsin()
	{
		// Arrange
		var symbolProfiles = new List<SymbolProfile>
		{
			new() { Symbol = "AAPL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock, ISIN = null },
			new() { Symbol = "GOOGL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock, ISIN = "US0231351067" }
		};

		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0231351067"))
			.ReturnsAsync((decimal?)1);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync(It.IsAny<string>()),
			Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldSetDefaultRatioWhenProviderReturnsNull()
	{
		// Arrange
		var symbolProfile = new SymbolProfile
		{
			Symbol = "AAPL",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US0378331005",
			SharesPerReceipt = 25m
		};

		var symbolProfiles = new List<SymbolProfile> { symbolProfile };
		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0378331005"))
			.ReturnsAsync((decimal?)null);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		symbolProfile.SharesPerReceipt.Should().Be(1m);
		mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		loggerMock.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated ADR/GDR ratio for AAPL from TEST_SOURCE: 25 -> 1")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldUpdateSharesPerReceiptWhenDifferent()
	{
		// Arrange
		var symbolProfile = new SymbolProfile
		{
			Symbol = "AAPL",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US0378331005",
			SharesPerReceipt = 1m
		};

		var symbolProfiles = new List<SymbolProfile> { symbolProfile };
		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0378331005"))
			.ReturnsAsync((decimal?)25);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		symbolProfile.SharesPerReceipt.Should().Be(25m);
		mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

		loggerMock.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated ADR/GDR ratio for AAPL from TEST_SOURCE: 1 -> 25")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldSkipUpdateWhenSharesPerReceiptIsSame()
	{
		// Arrange
		var symbolProfile = new SymbolProfile
		{
			Symbol = "AAPL",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US0378331005",
			SharesPerReceipt = 25m
		};

		var symbolProfiles = new List<SymbolProfile> { symbolProfile };
		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0378331005"))
			.ReturnsAsync((decimal?)25);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		symbolProfile.SharesPerReceipt.Should().Be(25m);
		mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

		loggerMock.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated ADR/GDR ratio")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
	}

	[Fact]
	public async Task DoWork_ShouldProcessMultipleSymbols()
	{
		// Arrange
		var symbolProfile1 = new SymbolProfile
		{
			Symbol = "AAPL",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US0378331005",
			SharesPerReceipt = 1m
		};

		var symbolProfile2 = new SymbolProfile
		{
			Symbol = "GOOGL",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US0231351067",
			SharesPerReceipt = 1m
		};

		var symbolProfiles = new List<SymbolProfile> { symbolProfile1, symbolProfile2 };
		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();
		var mockDbContext3 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);
		mockDbContext3.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext3.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object)
			.ReturnsAsync(mockDbContext3.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0378331005"))
			.ReturnsAsync((decimal?)25);
		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0231351067"))
			.ReturnsAsync((decimal?)2);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync(It.IsAny<string>()),
			Times.Exactly(2));

		symbolProfile1.SharesPerReceipt.Should().Be(25m);
		symbolProfile2.SharesPerReceipt.Should().Be(2m);
		mockDbContext2.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		mockDbContext3.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldLogUpdateMessage()
	{
		// Arrange
		var symbolProfile = new SymbolProfile
		{
			Symbol = "MSFT",
			DataSource = "TEST_SOURCE",
			AssetSubClass = AssetSubClass.Stock,
			ISIN = "US5949181045",
			SharesPerReceipt = 1m
		};

		var symbolProfiles = new List<SymbolProfile> { symbolProfile };
		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US5949181045"))
			.ReturnsAsync((decimal?)8);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		loggerMock.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated ADR/GDR ratio for MSFT from TEST_SOURCE: 1 -> 8")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task DoWork_ShouldSkipEmptyIsin()
	{
		// Arrange
		var symbolProfiles = new List<SymbolProfile>
		{
			new() { Symbol = "AAPL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock, ISIN = "" },
			new() { Symbol = "GOOGL", DataSource = "TEST_SOURCE", AssetSubClass = AssetSubClass.Stock, ISIN = "US0231351067" }
		};

		var holdings = new List<Holding>();

		var mockDbContext1 = new Mock<DatabaseContext>();
		var mockDbContext2 = new Mock<DatabaseContext>();

		mockDbContext1.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		mockDbContext2.Setup(db => db.Holdings).ReturnsDbSet(holdings);

		_mockDbContextFactory.SetupSequence(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockDbContext1.Object)
			.ReturnsAsync(mockDbContext2.Object);

		_mockAdrRatioProvider.Setup(p => p.GetSharesPerReceiptAsync("US0231351067"))
			.ReturnsAsync((decimal?)1);

		var loggerMock = new Mock<ILogger<MarketDataAdrRatioTask>>();

		// Act
		await _marketDataAdrRatioTask.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockAdrRatioProvider.Verify(
			p => p.GetSharesPerReceiptAsync("US0231351067"),
			Times.Once);
	}
}
