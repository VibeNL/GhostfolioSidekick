using AwesomeAssertions;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class GatherDividendsTaskTests
	{
		private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<IDividendRepository> _mockDividendRepository;
		private readonly GatherDividendsTask _task;

		public GatherDividendsTaskTests()
		{
			_mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_mockDividendRepository = new Mock<IDividendRepository>();
			_task = new GatherDividendsTask(_mockDbContextFactory.Object, _mockDividendRepository.Object);
		}

		[Fact]
		public void Priority_ShouldReturnMarketDataDividends()
		{
			_task.Priority.Should().Be(TaskPriority.MarketDataDividends);
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
			_task.Name.Should().Be("Gather Dividends Task");
		}

		[Fact]
		public async Task DoWork_WhenSymbolIsNotSupported_ShouldNotCallGetDividends()
		{
			// Arrange
			var symbolProfile = BuildSymbolProfile("AAPL");
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			_mockDividendRepository.Setup(r => r.IsSymbolSupported(It.IsAny<SymbolProfile>())).ReturnsAsync(false);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			_mockDividendRepository.Verify(r => r.GetDividends(It.IsAny<SymbolProfile>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_WhenSymbolIsNotSupported_ShouldLeaveExistingDividendsUnchanged()
		{
			// Arrange
			var existing = BuildDividend();
			var symbolProfile = BuildSymbolProfile("AAPL", [existing]);
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			_mockDividendRepository.Setup(r => r.IsSymbolSupported(It.IsAny<SymbolProfile>())).ReturnsAsync(false);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: dividends are untouched when the symbol is skipped
			symbolProfile.Dividends.Should().HaveCount(1);
		}

		[Fact]
		public async Task DoWork_WhenNewDividendIsGathered_ShouldAddItToSymbol()
		{
			// Arrange
			var symbolProfile = BuildSymbolProfile("AAPL");
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			var newDividend = BuildDividend();
			SetupSupportedSymbol("AAPL", [newDividend]);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			symbolProfile.Dividends.Should().HaveCount(1).And.Contain(newDividend);
		}

		[Fact]
		public async Task DoWork_WhenGatheredDividendMatchesExistingKey_ShouldUpdateAmountInPlace()
		{
			// Arrange: same (ExDividendDate, PaymentDate, DividendType, DividendState) key, different amount
			var existing = BuildDividend(amount: 1.00m);
			var symbolProfile = BuildSymbolProfile("AAPL", [existing]);
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			SetupSupportedSymbol("AAPL", [BuildDividend(amount: 1.75m)]);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: count stays at 1, amount is updated
			symbolProfile.Dividends.Should().HaveCount(1);
			symbolProfile.Dividends.Single().Amount.Amount.Should().Be(1.75m);
		}

		[Fact]
		public async Task DoWork_WhenExistingPaidDividendIsAbsentFromGathered_ShouldRemoveIt()
		{
			// Arrange
			var stale = BuildDividend(state: DividendState.Paid);
			var symbolProfile = BuildSymbolProfile("AAPL", [stale]);
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			SetupSupportedSymbol("AAPL", []);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: stale non-predicted dividend is removed
			symbolProfile.Dividends.Should().BeEmpty();
		}

		[Fact]
		public async Task DoWork_WhenExistingPredictedDividendIsAbsentFromGathered_ShouldPreserveIt()
		{
			// Arrange
			var predicted = BuildDividend(state: DividendState.Predicted);
			var symbolProfile = BuildSymbolProfile("AAPL", [predicted]);
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			SetupSupportedSymbol("AAPL", []);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert: Predicted dividends are never removed regardless of gathered data
			symbolProfile.Dividends.Should().HaveCount(1);
		}

		[Fact]
		public async Task DoWork_WhenMultipleSymbolsExist_ShouldOnlyProcessSupportedOnes()
		{
			// Arrange
			var symbolAapl = BuildSymbolProfile("AAPL");
			var symbolMsft = BuildSymbolProfile("MSFT");
			var (_, loggerMock) = SetupDbContext([symbolAapl, symbolMsft]);

			_mockDividendRepository
				.Setup(r => r.IsSymbolSupported(It.Is<SymbolProfile>(s => s.Symbol == "AAPL")))
				.ReturnsAsync(true);
			_mockDividendRepository
				.Setup(r => r.IsSymbolSupported(It.Is<SymbolProfile>(s => s.Symbol == "MSFT")))
				.ReturnsAsync(false);
			_mockDividendRepository
				.Setup(r => r.GetDividends(It.Is<SymbolProfile>(s => s.Symbol == "AAPL")))
				.ReturnsAsync((IList<Dividend>)[BuildDividend()]);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			symbolAapl.Dividends.Should().HaveCount(1);
			symbolMsft.Dividends.Should().BeEmpty();
			_mockDividendRepository.Verify(r => r.GetDividends(It.Is<SymbolProfile>(s => s.Symbol == "MSFT")), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldSaveChangesExactlyOnceAfterAllSymbolsAreProcessed()
		{
			// Arrange
			var symbolProfile = BuildSymbolProfile("AAPL");
			var (dbContextMock, loggerMock) = SetupDbContext([symbolProfile]);
			SetupSupportedSymbol("AAPL", []);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_WhenDividendsAreProcessed_ShouldLogUpsertedCount()
		{
			// Arrange
			var symbolProfile = BuildSymbolProfile("AAPL");
			var (_, loggerMock) = SetupDbContext([symbolProfile]);
			var gathered = new List<Dividend>
			{
				BuildDividend(),
				BuildDividend(exDividendDate: new DateOnly(2024, 4, 1), paymentDate: new DateOnly(2024, 4, 15)),
			};
			SetupSupportedSymbol("AAPL", gathered);

			// Act
			await _task.DoWork(loggerMock.Object);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Upserted 2 dividends for symbol AAPL")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		private (Mock<DatabaseContext> Context, Mock<ILogger> Logger) SetupDbContext(List<SymbolProfile> symbolProfiles)
		{
			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(db => db.SymbolProfiles).ReturnsDbSet(symbolProfiles);
			mockDbContext.Setup(db => db.Dividends).ReturnsDbSet(new List<Dividend>());
			mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

			_mockDbContextFactory
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(mockDbContext.Object);

			return (mockDbContext, new Mock<ILogger>());
		}

		private void SetupSupportedSymbol(string symbol, IList<Dividend> dividends)
		{
			_mockDividendRepository
				.Setup(r => r.IsSymbolSupported(It.Is<SymbolProfile>(s => s.Symbol == symbol)))
				.ReturnsAsync(true);
			_mockDividendRepository
				.Setup(r => r.GetDividends(It.Is<SymbolProfile>(s => s.Symbol == symbol)))
				.ReturnsAsync(dividends);
		}

		private static SymbolProfile BuildSymbolProfile(string symbol, List<Dividend>? dividends = null)
		{
			return new SymbolProfile
			{
				Symbol = symbol,
				DataSource = "YAHOO",
				Dividends = dividends ?? new List<Dividend>()
			};
		}

		private static Dividend BuildDividend(
			DateOnly? exDividendDate = null,
			DateOnly? paymentDate = null,
			DividendType type = DividendType.Cash,
			DividendState state = DividendState.Paid,
			decimal amount = 1.0m)
		{
			return new Dividend
			{
				ExDividendDate = exDividendDate ?? new DateOnly(2024, 1, 1),
				PaymentDate = paymentDate ?? new DateOnly(2024, 1, 15),
				DividendType = type,
				DividendState = state,
				Amount = new Money(Currency.USD, amount)
			};
		}
	}
}
