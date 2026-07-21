using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer;

public class GatherPriceTargetsTaskTests
{
	private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
	private readonly Mock<DatabaseContext> _mockContext;
	private readonly Mock<ITargetPriceRepository> _mockTargetPriceRepository;
	private readonly GatherPriceTargetsTask _task;

	public GatherPriceTargetsTaskTests()
	{
		_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
		_mockContext = new Mock<DatabaseContext>();
		_mockTargetPriceRepository = new Mock<ITargetPriceRepository>();
		_mockDbContextFactory
			.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(_mockContext.Object);
		_task = new GatherPriceTargetsTask(_mockDbContextFactory.Object, _mockTargetPriceRepository.Object);
	}

	[Fact]
	public void Priority_ShouldReturnMarketDataPriceTargets()
	{
		_task.Priority.Should().Be(TaskPriority.MarketDataPriceTargets);
	}

	[Fact]
	public void ExecutionFrequency_ShouldReturnHourly()
	{
		_task.ExecutionFrequency.Should().Be(TimeSpan.FromHours(1));
	}

	[Fact]
	public void ExceptionsAreFatal_ShouldReturnFalse()
	{
		_task.ExceptionsAreFatal.Should().BeFalse();
	}

	[Fact]
	public void Name_ShouldReturnCorrectName()
	{
		_task.Name.Should().Be("Gather Price Targets Task");
	}

	[Fact]
	public async Task DoWork_WhenNoSymbols_ShouldDoNothing()
	{
		// Arrange
		var (_, loggerMock) = SetupDbContext([]);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockTargetPriceRepository.Verify(r => r.GetPriceTarget(It.IsAny<SymbolProfile>()), Times.Never);
	}

	[Fact]
	public async Task DoWork_WhenSymbolNotTipRanks_ShouldSkipIt()
	{
		// Arrange
		var symbolProfile = BuildSymbolProfile("AAPL", Datasource.YAHOO);
		var (_, loggerMock) = SetupDbContext([symbolProfile]);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockTargetPriceRepository.Verify(r => r.GetPriceTarget(It.IsAny<SymbolProfile>()), Times.Never);
	}

	[Fact]
	public async Task DoWork_WhenPriceTargetReturned_ShouldSaveToDatabase()
	{
		// Arrange
		var symbolProfile = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var (dbContextMock, loggerMock) = SetupDbContext([symbolProfile]);
		var priceTarget = new PriceTarget
		{
			HighestTargetPriceAmount = 150m,
			AverageTargetPriceAmount = 120m,
			LowestTargetPriceAmount = 90m,
			Rating = AnalystRating.Buy
		};
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolProfile)).ReturnsAsync(priceTarget);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		dbContextMock.Verify(db => db.PriceTargets.Add(It.IsAny<PriceTarget>()), Times.Once);
		dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
	}

	[Fact]
	public async Task DoWork_WhenPriceTargetIsNull_ShouldNotSave()
	{
		// Arrange
		var symbolProfile = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var (dbContextMock, loggerMock) = SetupDbContext([symbolProfile]);
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolProfile)).ReturnsAsync((PriceTarget?)null);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		dbContextMock.Verify(db => db.PriceTargets.Add(It.IsAny<PriceTarget>()), Times.Never);
	}

	[Fact]
	public async Task DoWork_WhenExceptionThrown_ShouldLogAndContinue()
	{
		// Arrange
		var symbolProfile1 = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var symbolProfile2 = BuildSymbolProfile("MSFT", Datasource.TIPRANKS);
		var (_, loggerMock) = SetupDbContext([symbolProfile1, symbolProfile2]);
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolProfile1)).ThrowsAsync(new InvalidOperationException("API error"));

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		// Should not throw - exceptions are caught internally
		_mockTargetPriceRepository.Verify(r => r.GetPriceTarget(symbolProfile2), Times.Once);
	}

	[Fact]
	public async Task DoWork_WhenMultipleTipRanksSymbols_ShouldProcessAll()
	{
		// Arrange
		var symbolAapl = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var symbolMsft = BuildSymbolProfile("MSFT", Datasource.TIPRANKS);
		var symbolYahoo = BuildSymbolProfile("GOOG", Datasource.YAHOO);
		var (dbContextMock, loggerMock) = SetupDbContext([symbolAapl, symbolMsft, symbolYahoo]);

		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolAapl)).ReturnsAsync(new PriceTarget { Rating = AnalystRating.StrongBuy });
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolMsft)).ReturnsAsync(new PriceTarget { Rating = AnalystRating.Hold });

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		_mockTargetPriceRepository.Verify(r => r.GetPriceTarget(symbolAapl), Times.Once);
		_mockTargetPriceRepository.Verify(r => r.GetPriceTarget(symbolMsft), Times.Once);
		dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
	}

	[Fact]
	public async Task DoWork_WhenPriceTargetSaved_ShouldSetSymbolProperty()
	{
		// Arrange
		var symbolProfile = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var (_, loggerMock) = SetupDbContext([symbolProfile]);
		var priceTarget = new PriceTarget { Rating = AnalystRating.Buy };
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolProfile)).ReturnsAsync(priceTarget);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert
		priceTarget.Symbol.Should().Be("AAPL");
	}

	[Fact]
	public async Task DoWork_ShouldCallDbContextFactoryExactlyThreeTimesPerSymbol()
	{
		// Arrange
		var symbolProfile = BuildSymbolProfile("AAPL", Datasource.TIPRANKS);
		var (_, loggerMock) = SetupDbContext([symbolProfile]);
		var priceTarget = new PriceTarget { Rating = AnalystRating.Buy };
		_mockTargetPriceRepository.Setup(r => r.GetPriceTarget(symbolProfile)).ReturnsAsync(priceTarget);

		// Act
		await _task.DoWork(loggerMock.Object, CancellationToken.None);

		// Assert — 1 for symbol query + 1 for ClearPriceTargets + 1 for saving price target
		_mockDbContextFactory.Verify(
			f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()),
			Times.Exactly(3),
			"DbContext should be created once for the symbol query, once for clearing old targets, and once for saving the new target");
	}

	private (Mock<DatabaseContext> Context, Mock<ILogger> LoggerMock) SetupDbContext(List<SymbolProfile> symbolProfiles)
	{
		_mockContext.Reset();
		_mockContext.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
		_mockContext.Setup(db => db.PriceTargets).ReturnsDbSet(new List<PriceTarget>());
		_mockContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

		return (_mockContext, new Mock<ILogger>());
	}

	private static SymbolProfile BuildSymbolProfile(string symbol, string dataSource)
	{
		return new SymbolProfile
		{
			Symbol = symbol,
			DataSource = dataSource
		};
	}
}
