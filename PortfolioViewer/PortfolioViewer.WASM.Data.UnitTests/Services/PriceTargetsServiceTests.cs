using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services;

public class PriceTargetsServiceTests
{
    [Fact]
    public async Task GetPriceTargetsAsync_WhenNoData_ReturnsEmptyList()
    {
        // Arrange
        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(new List<PriceTarget>());

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPriceTargetsAsync_WhenDataExists_ReturnsMappedModels()
    {
        // Arrange
        var priceTargets = new List<PriceTarget>
        {
            new()
            {
                Symbol = "AAPL",
                HighestTargetPriceAmount = 200m,
                HighestTargetCurrency = new Currency { Symbol = "USD" },
                AverageTargetPriceAmount = 180m,
                AverageTargetCurrency = new Currency { Symbol = "USD" },
                LowestTargetPriceAmount = 150m,
                LowestTargetCurrency = new Currency { Symbol = "USD" },
                Rating = AnalystRating.Buy,
                NumberOfBuys = 10,
                NumberOfHolds = 5,
                NumberOfSells = 2
            },
            new()
            {
                Symbol = "MSFT",
                HighestTargetPriceAmount = 450m,
                HighestTargetCurrency = new Currency { Symbol = "USD" },
                AverageTargetPriceAmount = 400m,
                AverageTargetCurrency = new Currency { Symbol = "USD" },
                LowestTargetPriceAmount = 350m,
                LowestTargetCurrency = new Currency { Symbol = "USD" },
                Rating = AnalystRating.Hold,
                NumberOfBuys = 3,
                NumberOfHolds = 15,
                NumberOfSells = 8
            }
        };

        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(priceTargets);

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("MSFT", result[1].Symbol);
        Assert.Equal(200m, result[0].HighestTargetAmount);
        Assert.Equal(180m, result[0].AverageTargetAmount);
        Assert.Equal(150m, result[0].LowestTargetAmount);
        Assert.Equal("Buy", result[0].Rating);
        Assert.Equal(10, result[0].NumberOfBuys);
        Assert.Equal(5, result[0].NumberOfHolds);
        Assert.Equal(2, result[0].NumberOfSells);
    }

    [Fact]
    public async Task GetPriceTargetsAsync_ExcludesZeroAverageTargets()
    {
        // Arrange
        var priceTargets = new List<PriceTarget>
        {
            new()
            {
                Symbol = "AAPL",
                AverageTargetPriceAmount = 180m,
                AverageTargetCurrency = new Currency { Symbol = "USD" }
            },
            new()
            {
                Symbol = "GOOG",
                AverageTargetPriceAmount = 0m,
                AverageTargetCurrency = new Currency { Symbol = "USD" }
            }
        };

        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(priceTargets);

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("AAPL", result[0].Symbol);
    }

    [Fact]
    public async Task GetPriceTargetsAsync_WhenCurrencyIsNull_ReturnsUsdDefault()
    {
        // Arrange
        var priceTarget = new PriceTarget
        {
            Symbol = "AAPL",
            HighestTargetPriceAmount = 200m,
            HighestTargetCurrency = null!,
            AverageTargetPriceAmount = 180m,
            AverageTargetCurrency = null!,
            LowestTargetPriceAmount = 150m,
            LowestTargetCurrency = null!,
            Rating = AnalystRating.Buy,
            NumberOfBuys = 10,
            NumberOfHolds = 5,
            NumberOfSells = 2
        };

        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(new List<PriceTarget> { priceTarget });

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("USD", result[0].HighestTargetCurrency);
        Assert.Equal("USD", result[0].AverageTargetCurrency);
        Assert.Equal("USD", result[0].LowestTargetCurrency);
    }

    [Fact]
    public async Task GetPriceTargetForSymbol_WhenFound_ReturnsMappedModel()
    {
        // Arrange
        var priceTarget = new PriceTarget
        {
            Symbol = "AAPL",
            HighestTargetPriceAmount = 200m,
            HighestTargetCurrency = new Currency { Symbol = "USD" },
            AverageTargetPriceAmount = 180m,
            AverageTargetCurrency = new Currency { Symbol = "USD" },
            LowestTargetPriceAmount = 150m,
            LowestTargetCurrency = new Currency { Symbol = "USD" },
            Rating = AnalystRating.StrongBuy,
            NumberOfBuys = 15,
            NumberOfHolds = 3,
            NumberOfSells = 1
        };

        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(new List<PriceTarget> { priceTarget });

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetForSymbolAsync("AAPL", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AAPL", result!.Symbol);
        Assert.Equal(200m, result.HighestTargetAmount);
        Assert.Equal(180m, result.AverageTargetAmount);
        Assert.Equal(150m, result.LowestTargetAmount);
        Assert.Equal("StrongBuy", result.Rating);
        Assert.Equal(15, result.NumberOfBuys);
        Assert.Equal(3, result.NumberOfHolds);
        Assert.Equal(1, result.NumberOfSells);
    }

    [Fact]
    public async Task GetPriceTargetForSymbol_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(new List<PriceTarget>());

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetForSymbolAsync("NONEXISTENT", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPriceTargetsAsync_ResultsAreSortedBySymbol()
    {
        // Arrange
        var priceTargets = new List<PriceTarget>
        {
            new()
            {
                Symbol = "ZBRA",
                AverageTargetPriceAmount = 250m,
                AverageTargetCurrency = new Currency { Symbol = "USD" }
            },
            new()
            {
                Symbol = "AAPL",
                AverageTargetPriceAmount = 180m,
                AverageTargetCurrency = new Currency { Symbol = "USD" }
            },
            new()
            {
                Symbol = "MSFT",
                AverageTargetPriceAmount = 400m,
                AverageTargetCurrency = new Currency { Symbol = "USD" }
            }
        };

        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(priceTargets);

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("AAPL", result[0].Symbol);
        Assert.Equal("MSFT", result[1].Symbol);
        Assert.Equal("ZBRA", result[2].Symbol);
    }

    [Fact]
    public async Task GetPriceTargetsAsync_CancelsOnCancellationRequested()
    {
        // Arrange
        var mockContext = new Mock<DatabaseContext>();
        mockContext.Setup(x => x.PriceTargets).ReturnsDbSet(new List<PriceTarget>());

        var mockFactory = new Mock<IDbContextFactory<DatabaseContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContext.Object);

        var service = new PriceTargetsService(mockFactory.Object);

        // Act
        var result = await service.GetPriceTargetsAsync(new CancellationToken(canceled: true));

        // Assert
        Assert.Empty(result);
    }
}
