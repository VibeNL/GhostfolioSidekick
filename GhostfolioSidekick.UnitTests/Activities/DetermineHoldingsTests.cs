using AwesomeAssertions;
using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class DetermineHoldingsTests
	{
		private readonly Mock<ILogger<DetermineHoldings>> _loggerMock;
		private readonly Mock<IDbContextFactory<DatabaseContext>> _dbContextFactoryMock;
		private readonly IMemoryCache _memoryCacheMock;
		private readonly Mock<ISymbolMatcher> _symbolMatcherMock;
		private readonly List<ISymbolMatcher> _symbolMatchers;
		private readonly DetermineHoldings _determineHoldings;

		public DetermineHoldingsTests()
		{
			_loggerMock = new Mock<ILogger<DetermineHoldings>>();
			_dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			_memoryCacheMock = new MemoryCache(new MemoryCacheOptions());
			_symbolMatcherMock = new Mock<ISymbolMatcher>();
			_symbolMatchers = [_symbolMatcherMock.Object];
			var _mockApplicationSettings = new Mock<IApplicationSettings>();

			_determineHoldings = new DetermineHoldings(
				[.. _symbolMatchers],
				_dbContextFactoryMock.Object,
				_memoryCacheMock,
				_mockApplicationSettings.Object);
		}

		[Fact]
		public void Priority_ShouldReturnDetermineHoldings()
		{
			// Act
			var priority = _determineHoldings.Priority;

			// Assert
			priority.Should().Be(TaskPriority.DetermineHoldings);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnDaily()
		{
			// Act
			var frequency = _determineHoldings.ExecutionFrequency;

			// Assert
			frequency.Should().Be(Frequencies.Daily);
		}

		[Fact]
		public void ExceptionsAreFatal_ShouldReturnFalse()
		{
			// Act
			var exceptionsAreFatal = _determineHoldings.ExceptionsAreFatal;

			// Assert
			exceptionsAreFatal.Should().BeFalse();
		}

		[Fact]
		public async Task DoWork_ShouldRemoveHoldingsWithoutSymbolProfiles()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>();
			var holdings = new List<Holding>
			{
				new() { Id =1, SymbolProfiles = [] },
				new() { Id =2, SymbolProfiles = [new SymbolProfile()] }
			};

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContext()).Returns(dbContextMock.Object);

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Remove(It.Is<Holding>(h => h.Id ==1)), Times.Once);
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldCreateNewHoldings()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContext()).Returns(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(new SymbolProfile { Symbol = "TEST", DataSource = "TestSource" });

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.IsAny<Holding>()), Times.Once);
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldLogHoldingAlreadyExistsForSymbol_WhenSymbolHoldingDictionaryContainsSymbol()
		{
			// Arrange - This test targets line 119
			var dbContextMock = new Mock<DatabaseContext>();
			var symbolProfile = new SymbolProfile
			{
				Symbol = "TEST",
				DataSource = "TestSource",
				Currency = Currency.USD,
				Name = "Test Symbol"
			};

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST1")] },
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST2")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContext()).Returns(dbContextMock.Object);

			// Return the same symbol profile for both partial identifiers
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(symbolProfile);

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			// Verify that the log message for "holding already exists for symbol" was called
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Trace,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateOrUpdateHolding: Holding already exists for") &&
													v.ToString()!.Contains("TEST") &&
													v.ToString()!.Contains("with")),
					It.IsAny<Exception?>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldLogNoSymbolProfileFound_WhenNoSymbolMatcherReturnsSymbol()
		{
			// Arrange - This test targets line 148
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("UNKNOWN")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContext()).Returns(dbContextMock.Object);

			// Return null for all symbol matches
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync((SymbolProfile?)null);

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			// Verify that the log warning for "no symbol profile found" was called
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateOrUpdateHolding: No symbol profile found for") &&
													v.ToString()!.Contains("UNKNOWN")),
					It.IsAny<Exception?>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		private record TestActivity : ActivityWithQuantityAndUnitPrice
		{
			public TestActivity()
			{
			}
		}
	}
}
