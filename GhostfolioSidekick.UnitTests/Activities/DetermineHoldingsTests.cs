using GhostfolioSidekick.Activities.Comparer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GhostfolioSidekick.UnitTests.Activities
{
    public class DetermineHoldingsTests
    {
        private readonly Mock<ILogger<DetermineHoldings>> _loggerMock;
        private readonly Mock<ISymbolMatcher> _symbolMatcherMock;
        private readonly Mock<IDbContextFactory<DatabaseContext>> _dbContextFactoryMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly DetermineHoldings _determineHoldings;

        public DetermineHoldingsTests()
        {
            _loggerMock = new Mock<ILogger<DetermineHoldings>>();
            _symbolMatcherMock = new Mock<ISymbolMatcher>();
            _dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
            _memoryCacheMock = new Mock<IMemoryCache>();

            _determineHoldings = new DetermineHoldings(
                _loggerMock.Object,
                new[] { _symbolMatcherMock.Object },
                _dbContextFactoryMock.Object,
                _memoryCacheMock.Object);
        }

        [Fact]
        public void Priority_ShouldReturnDetermineHoldings()
        {
            // Act
            var priority = _determineHoldings.Priority;

            // Assert
            Assert.Equal(TaskPriority.DetermineHoldings, priority);
        }

        [Fact]
        public async Task DoWork_ShouldCreateOrUpdateHoldings()
        {
            // Arrange
            var activities = new List<Activity>
            {
                new BuySellActivity { PartialSymbolIdentifiers = new List<PartialSymbolIdentifier> { new PartialSymbolIdentifier { Identifier = "AAPL" } } }
            };

            var holdings = new List<Holding>();

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(activities);
            dbContextMock.Setup(x => x.Holdings).ReturnsDbSet(holdings);

            _dbContextFactoryMock.Setup(x => x.CreateDbContext()).Returns(dbContextMock.Object);

            _symbolMatcherMock.Setup(x => x.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(new SymbolProfile { Symbol = "AAPL" });

            // Act
            await _determineHoldings.DoWork();

            // Assert
            Assert.Single(holdings);
            Assert.Equal("AAPL", holdings.First().SymbolProfiles.First().Symbol);
        }

        [Fact]
        public async Task DoWork_ShouldRemoveHoldingsWithoutSymbolProfiles()
        {
            // Arrange
            var holdings = new List<Holding>
            {
                new Holding { SymbolProfiles = new List<SymbolProfile>() }
            };

            var dbContextMock = new Mock<DatabaseContext>();
            dbContextMock.Setup(x => x.Activities).ReturnsDbSet(new List<Activity>());
            dbContextMock.Setup(x => x.Holdings).ReturnsDbSet(holdings);

            _dbContextFactoryMock.Setup(x => x.CreateDbContext()).Returns(dbContextMock.Object);

            // Act
            await _determineHoldings.DoWork();

            // Assert
            Assert.Empty(holdings);
        }
    }
}
