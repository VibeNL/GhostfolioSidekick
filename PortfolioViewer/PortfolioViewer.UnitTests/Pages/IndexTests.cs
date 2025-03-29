using Bunit;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PortfolioViewer.Pages;
using Xunit;

namespace PortfolioViewer.UnitTests.Pages
{
    public class IndexTests : TestContext
    {
        private readonly Mock<IApiWrapper> _apiWrapperMock;
        private readonly Mock<IDbContextFactory<DatabaseContext>> _dbContextFactoryMock;
        private readonly List<Activity> _activities;
        private readonly List<SymbolProfile> _symbolProfiles;

        public IndexTests()
        {
            _apiWrapperMock = new Mock<IApiWrapper>();
            _dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
            _activities = new List<Activity>
            {
                new Activity
                {
                    Date = DateTime.Now,
                    Type = ActivityType.Buy,
                    SymbolProfile = new SymbolProfile { Symbol = "AAPL", Name = "Apple Inc.", AssetClass = "Equity", Currency = "USD" },
                    Quantity = 10,
                    UnitPrice = 150
                }
            };
            _symbolProfiles = new List<SymbolProfile>
            {
                new SymbolProfile { Symbol = "AAPL", Name = "Apple Inc.", AssetClass = "Equity", Currency = "USD" }
            };

            Services.AddSingleton(_apiWrapperMock.Object);
            Services.AddSingleton(_dbContextFactoryMock.Object);
        }

        [Fact]
        public void Index_ShouldDisplayActivitiesAndSymbolProfiles()
        {
            // Arrange
            _dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync())
                .ReturnsAsync(MockDbContext());

            // Act
            var cut = RenderComponent<Index>();

            // Assert
            cut.Markup.Should().Contain("Activities");
            cut.Markup.Should().Contain("Symbol Profiles");
            cut.Markup.Should().Contain("Apple Inc.");
        }

        private DatabaseContext MockDbContext()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var context = new DatabaseContext(options);
            context.Activities.AddRange(_activities);
            context.SymbolProfiles.AddRange(_symbolProfiles);
            context.SaveChanges();

            return context;
        }
    }
}
