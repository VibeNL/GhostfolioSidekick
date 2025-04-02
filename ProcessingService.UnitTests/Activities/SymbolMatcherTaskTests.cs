using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities.Types;
using Moq.EntityFrameworkCore;
using GhostfolioSidekick.ProcessingService.Activities;

namespace GhostfolioSidekick.ProcessingService.UnitTests.Activities
{
	public class SymbolMatcherTaskTests
    {
        private readonly Mock<ILogger<SymbolMatcherTask>> _mockLogger;
        private readonly Mock<IApplicationSettings> _mockApplicationSettings;
        private readonly Mock<IDbContextFactory<DatabaseContext>> _mockDbContextFactory;
		private readonly Mock<ISymbolMatcher> _symbolMatcher;
		private readonly List<ISymbolMatcher> _symbolMatchers;
        private readonly SymbolMatcherTask _symbolMatcherTask;

        public SymbolMatcherTaskTests()
        {
            _mockLogger = new Mock<ILogger<SymbolMatcherTask>>();
            _mockApplicationSettings = new Mock<IApplicationSettings>();
            _mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			_symbolMatcher = new Mock<ISymbolMatcher>();
			_symbolMatchers = new List<ISymbolMatcher>
			{
				_symbolMatcher.Object
            };

            _symbolMatcherTask = new SymbolMatcherTask(
                _mockLogger.Object,
                _mockApplicationSettings.Object,
                _symbolMatchers.ToArray(),
                _mockDbContextFactory.Object);
        }

        [Fact]
        public void Priority_ShouldReturnSymbolMatcher()
        {
            // Act
            var priority = _symbolMatcherTask.Priority;

            // Assert
            priority.Should().Be(TaskPriority.SymbolMatcher);
        }

        [Fact]
        public void ExecutionFrequency_ShouldReturnOneHour()
        {
            // Act
            var frequency = _symbolMatcherTask.ExecutionFrequency;

            // Assert
            frequency.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public async Task DoWork_ShouldMatchSymbolsAndSaveChanges()
        {
            // Arrange
            var activities = new List<Activity>
            {
                new BuySellActivity { Date = DateTime.Now, PartialSymbolIdentifiers = new List<PartialSymbolIdentifier> { new PartialSymbolIdentifier { Identifier = "SYM1" } } }
            };

            var holdings = new List<Holding>();

            var mockDbContext = new Mock<DatabaseContext>();
            mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);
            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

			_symbolMatcher.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(new SymbolProfile { Symbol = "SYM1", DataSource = "TestSource" });
			_symbolMatcher.Setup(sm => sm.DataSource).Returns("TestSource");

            // Act
            await _symbolMatcherTask.DoWork();

			// Assert
			_symbolMatcher.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Once);
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DoWork_ShouldSkipMatching_WhenNoPartialIdentifiers()
        {
            // Arrange
            var activities = new List<Activity>
            {
                new BuySellActivity { Date = DateTime.Now, PartialSymbolIdentifiers = new List<PartialSymbolIdentifier>() }
            };

            var holdings = new List<Holding>();

            var mockDbContext = new Mock<DatabaseContext>();
            mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);
            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

            var mockSymbolMatcher = new Mock<ISymbolMatcher>();
            _symbolMatchers.Clear();
            _symbolMatchers.Add(mockSymbolMatcher.Object);

            // Act
            await _symbolMatcherTask.DoWork();

            // Assert
            mockSymbolMatcher.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Never);
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DoWork_ShouldSkipMatching_WhenSymbolAlreadyMatched()
        {
            // Arrange
            var activities = new List<Activity>
            {
                new BuySellActivity { Date = DateTime.Now, PartialSymbolIdentifiers = new List<PartialSymbolIdentifier> { new PartialSymbolIdentifier { Identifier = "SYM1" } } }
            };

            var holdings = new List<Holding>
            {
                new Holding { SymbolProfiles = new List<SymbolProfile> { new SymbolProfile { Symbol = "SYM1", DataSource = "TestSource" } } }
            };

            var mockDbContext = new Mock<DatabaseContext>();
            mockDbContext.Setup(db => db.Activities).ReturnsDbSet(activities);
            mockDbContext.Setup(db => db.Holdings).ReturnsDbSet(holdings);
            _mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(mockDbContext.Object);

            var mockSymbolMatcher = new Mock<ISymbolMatcher>();
            _symbolMatchers.Clear();
            _symbolMatchers.Add(mockSymbolMatcher.Object);

            // Act
            await _symbolMatcherTask.DoWork();

            // Assert
            mockSymbolMatcher.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Never);
            mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
        }
    }
}

