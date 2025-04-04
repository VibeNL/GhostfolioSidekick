using FluentAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.ProcessingService.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.ProcessingService.UnitTests.Activities
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
			_symbolMatchers = new List<ISymbolMatcher> { _symbolMatcherMock.Object };

			_determineHoldings = new DetermineHoldings(
				_loggerMock.Object,
				_symbolMatchers.ToArray(),
				_dbContextFactoryMock.Object,
				_memoryCacheMock);
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
				new Holding { Id = 1, SymbolProfiles = new List<SymbolProfile>() },
				new Holding { Id = 2, SymbolProfiles = [new SymbolProfile()] }
			};

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContext()).Returns(dbContextMock.Object);

			// Act
			await _determineHoldings.DoWork();

			// Assert
			dbContextMock.Verify(db => db.Holdings.Remove(It.Is<Holding>(h => h.Id == 1)), Times.Once);
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

			// Act
			await _determineHoldings.DoWork();

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.IsAny<Holding>()), Times.Once);
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Once);
		}

		private record TestActivity : ActivityWithQuantityAndUnitPrice
		{
			public TestActivity()
			{
			}
		}
	}
}
