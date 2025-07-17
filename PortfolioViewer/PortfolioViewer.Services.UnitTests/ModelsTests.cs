using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class PortfolioModelsTests
{
    [Fact]
    public void PortfolioValuePoint_ShouldInitializeWithDefaultValues()
    {
        // Act
        var point = new PortfolioValuePoint();

        // Assert
        Assert.Equal(DateTime.MinValue, point.Date);
        Assert.Equal(0m, point.TotalValue);
        Assert.Equal(0m, point.CashValue);
        Assert.Equal(0m, point.HoldingsValue);
        Assert.Equal(0m, point.CumulativeInvested);
    }

    [Fact]
    public void PortfolioValuePoint_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var date = DateTime.Today;
        var totalValue = 10000m;
        var cashValue = 3000m;
        var holdingsValue = 7000m;
        var cumulativeInvested = 9500m;

        // Act
        var point = new PortfolioValuePoint
        {
            Date = date,
            TotalValue = totalValue,
            CashValue = cashValue,
            HoldingsValue = holdingsValue,
            CumulativeInvested = cumulativeInvested
        };

        // Assert
        Assert.Equal(date, point.Date);
        Assert.Equal(totalValue, point.TotalValue);
        Assert.Equal(cashValue, point.CashValue);
        Assert.Equal(holdingsValue, point.HoldingsValue);
        Assert.Equal(cumulativeInvested, point.CumulativeInvested);
    }

    [Fact]
    public void CashFlowPoint_ShouldInitializeWithDefaultValues()
    {
        // Act
        var point = new CashFlowPoint();

        // Assert
        Assert.Equal(DateTime.MinValue, point.Date);
        Assert.Equal(0m, point.Amount);
        Assert.Equal(string.Empty, point.Currency);
        Assert.False(point.IsDeposit);
    }

    [Fact]
    public void CashFlowPoint_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var date = DateTime.Today;
        var amount = 1000m;
        var currency = "USD";
        var isDeposit = true;

        // Act
        var point = new CashFlowPoint
        {
            Date = date,
            Amount = amount,
            Currency = currency,
            IsDeposit = isDeposit
        };

        // Assert
        Assert.Equal(date, point.Date);
        Assert.Equal(amount, point.Amount);
        Assert.Equal(currency, point.Currency);
        Assert.Equal(isDeposit, point.IsDeposit);
    }

    [Fact]
    public void BalancePoint_ShouldInitializeWithDefaultValues()
    {
        // Act
        var point = new BalancePoint();

        // Assert
        Assert.Equal(DateTime.MinValue, point.Date);
        Assert.Equal(0m, point.Amount);
        Assert.Equal(0, point.AccountId);
    }

    [Fact]
    public void HoldingValuePoint_ShouldInitializeWithDefaultValues()
    {
        // Act
        var point = new HoldingValuePoint();

        // Assert
        Assert.Equal(DateTime.MinValue, point.Date);
        Assert.Equal(0m, point.Value);
        Assert.Equal(0, point.HoldingId);
        Assert.Equal(0m, point.Quantity);
        Assert.Equal(0m, point.Price);
    }

    [Fact]
    public void HoldingValuePoint_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var date = DateTime.Today;
        var value = 5000m;
        var holdingId = 123;
        var quantity = 100m;
        var price = 50m;

        // Act
        var point = new HoldingValuePoint
        {
            Date = date,
            Value = value,
            HoldingId = holdingId,
            Quantity = quantity,
            Price = price
        };

        // Assert
        Assert.Equal(date, point.Date);
        Assert.Equal(value, point.Value);
        Assert.Equal(holdingId, point.HoldingId);
        Assert.Equal(quantity, point.Quantity);
        Assert.Equal(price, point.Price);
    }

    [Fact]
    public void AccountBreakdown_ShouldInitializeWithDefaultValues()
    {
        // Act
        var breakdown = new AccountBreakdown();

        // Assert
        Assert.Equal(string.Empty, breakdown.AccountName);
        Assert.Equal(0m, breakdown.CurrentValue);
        Assert.Equal(0m, breakdown.CashBalance);
        Assert.Equal(0m, breakdown.HoldingsValue);
        Assert.Equal(0m, breakdown.PercentageOfPortfolio);
    }

    [Fact]
    public void AccountBreakdown_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var accountName = "Test Account";
        var currentValue = 15000m;
        var cashBalance = 5000m;
        var holdingsValue = 10000m;
        var percentage = 25.5m;

        // Act
        var breakdown = new AccountBreakdown
        {
            AccountName = accountName,
            CurrentValue = currentValue,
            CashBalance = cashBalance,
            HoldingsValue = holdingsValue,
            PercentageOfPortfolio = percentage
        };

        // Assert
        Assert.Equal(accountName, breakdown.AccountName);
        Assert.Equal(currentValue, breakdown.CurrentValue);
        Assert.Equal(cashBalance, breakdown.CashBalance);
        Assert.Equal(holdingsValue, breakdown.HoldingsValue);
        Assert.Equal(percentage, breakdown.PercentageOfPortfolio);
    }

    [Fact]
    public void PortfolioSummary_ShouldInitializeWithDefaultValues()
    {
        // Act
        var summary = new PortfolioSummary();

        // Assert
        Assert.Equal("N/A", summary.CurrentPortfolioValue);
        Assert.Equal("N/A", summary.CurrentValueDate);
        Assert.Equal(0m, summary.TotalReturnAmount);
        Assert.Equal(0m, summary.TotalReturnPercent);
        Assert.Equal("N/A", summary.TotalInvestedAmount);
    }

    [Fact]
    public void PortfolioSummary_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var currentValue = "10,000.00 USD";
        var currentDate = "Jan 15, 2024";
        var returnAmount = 1500m;
        var returnPercent = 17.5m;
        var investedAmount = "8,500.00 USD";

        // Act
        var summary = new PortfolioSummary
        {
            CurrentPortfolioValue = currentValue,
            CurrentValueDate = currentDate,
            TotalReturnAmount = returnAmount,
            TotalReturnPercent = returnPercent,
            TotalInvestedAmount = investedAmount
        };

        // Assert
        Assert.Equal(currentValue, summary.CurrentPortfolioValue);
        Assert.Equal(currentDate, summary.CurrentValueDate);
        Assert.Equal(returnAmount, summary.TotalReturnAmount);
        Assert.Equal(returnPercent, summary.TotalReturnPercent);
        Assert.Equal(investedAmount, summary.TotalInvestedAmount);
    }
}

public class AnalyticsModelsTests
{
    [Fact]
    public void MonthlyData_ShouldInitializeWithDefaultValues()
    {
        // Act
        var data = new MonthlyData();

        // Assert
        Assert.Equal(string.Empty, data.Month);
        Assert.Equal(0, data.Count);
    }

    [Fact]
    public void BuySellVolumeAnalysis_ShouldInitializeWithDefaultValues()
    {
        // Act
        var analysis = new BuySellVolumeAnalysis();

        // Assert
        Assert.Equal(string.Empty, analysis.Currency);
        Assert.Equal(0m, analysis.TotalBuyVolume);
        Assert.Equal(0m, analysis.TotalSellVolume);
        Assert.Equal(0, analysis.TransactionCount);
    }

    [Fact]
    public void BuySellVolumeAnalysis_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var currency = "USD";
        var buyVolume = 50000m;
        var sellVolume = 30000m;
        var transactionCount = 25;

        // Act
        var analysis = new BuySellVolumeAnalysis
        {
            Currency = currency,
            TotalBuyVolume = buyVolume,
            TotalSellVolume = sellVolume,
            TransactionCount = transactionCount
        };

        // Assert
        Assert.Equal(currency, analysis.Currency);
        Assert.Equal(buyVolume, analysis.TotalBuyVolume);
        Assert.Equal(sellVolume, analysis.TotalSellVolume);
        Assert.Equal(transactionCount, analysis.TransactionCount);
    }

    [Fact]
    public void DividendIncomeAnalysis_ShouldInitializeWithDefaultValues()
    {
        // Act
        var analysis = new DividendIncomeAnalysis();

        // Assert
        Assert.Equal(string.Empty, analysis.Currency);
        Assert.Equal(0m, analysis.TotalAmount);
        Assert.Equal(0m, analysis.AveragePayment);
        Assert.Equal(0, analysis.PaymentCount);
    }

    [Fact]
    public void DividendIncomeAnalysis_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var currency = "EUR";
        var totalAmount = 1200m;
        var averagePayment = 100m;
        var paymentCount = 12;

        // Act
        var analysis = new DividendIncomeAnalysis
        {
            Currency = currency,
            TotalAmount = totalAmount,
            AveragePayment = averagePayment,
            PaymentCount = paymentCount
        };

        // Assert
        Assert.Equal(currency, analysis.Currency);
        Assert.Equal(totalAmount, analysis.TotalAmount);
        Assert.Equal(averagePayment, analysis.AveragePayment);
        Assert.Equal(paymentCount, analysis.PaymentCount);
    }

    [Fact]
    public void HoldingActivitySummary_ShouldInitializeWithDefaultValues()
    {
        // Act
        var summary = new HoldingActivitySummary();

        // Assert
        Assert.Equal(string.Empty, summary.Symbol);
        Assert.Equal(string.Empty, summary.AssetClass);
        Assert.Equal(0, summary.ActivityCount);
        Assert.Null(summary.LatestActivity);
        Assert.NotNull(summary.ActivityTypes);
        Assert.Empty(summary.ActivityTypes);
    }

    [Fact]
    public void HoldingActivitySummary_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var symbol = "AAPL";
        var assetClass = "EQUITY";
        var activityCount = 15;
        var latestActivity = DateTime.Today;
        var activityTypes = new List<string> { "Buy/Sell", "Dividend" };

        // Act
        var summary = new HoldingActivitySummary
        {
            Symbol = symbol,
            AssetClass = assetClass,
            ActivityCount = activityCount,
            LatestActivity = latestActivity,
            ActivityTypes = activityTypes
        };

        // Assert
        Assert.Equal(symbol, summary.Symbol);
        Assert.Equal(assetClass, summary.AssetClass);
        Assert.Equal(activityCount, summary.ActivityCount);
        Assert.Equal(latestActivity, summary.LatestActivity);
        Assert.Equal(activityTypes, summary.ActivityTypes);
    }

    [Fact]
    public void TimelineItem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var item = new TimelineItem();

        // Assert
        Assert.Equal(DateTime.MinValue, item.Date);
        Assert.Equal(string.Empty, item.ActivityType);
        Assert.Equal(string.Empty, item.Account);
        Assert.Equal(string.Empty, item.Description);
    }

    [Fact]
    public void AccountSummary_ShouldInitializeWithDefaultValues()
    {
        // Act
        var summary = new AccountSummary();

        // Assert
        Assert.Equal(string.Empty, summary.Name);
        Assert.Null(summary.Platform);
        Assert.Equal(0, summary.ActivitiesCount);
        Assert.Equal(string.Empty, summary.LatestBalanceDisplay);
    }

    [Fact]
    public void AccountSummary_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var name = "Investment Account";
        var platform = new Platform("Test Broker");
        var activitiesCount = 25;
        var balanceDisplay = "15,000.00 USD";

        // Act
        var summary = new AccountSummary
        {
            Name = name,
            Platform = platform,
            ActivitiesCount = activitiesCount,
            LatestBalanceDisplay = balanceDisplay
        };

        // Assert
        Assert.Equal(name, summary.Name);
        Assert.Equal(platform, summary.Platform);
        Assert.Equal(activitiesCount, summary.ActivitiesCount);
        Assert.Equal(balanceDisplay, summary.LatestBalanceDisplay);
    }
}

public class HoldingsModelsTests
{
    [Fact]
    public void HoldingPerformanceData_ShouldInitializeWithDefaultValues()
    {
        // Act
        var data = new HoldingPerformanceData();

        // Assert
        Assert.Equal(string.Empty, data.Symbol);
        Assert.Equal(string.Empty, data.Name);
        Assert.Equal(string.Empty, data.AssetClass);
        Assert.Null(data.AssetSubClass);
        Assert.Equal(string.Empty, data.DataSource);
        Assert.Null(data.ISIN);
        Assert.Equal(0, data.ActivityCount);
        Assert.Equal(0, data.MarketDataCount);
        Assert.Null(data.LatestPrice);
        Assert.Null(data.LatestPriceDate);
        Assert.Equal(0m, data.TotalQuantity);
    }

    [Fact]
    public void HoldingPerformanceData_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var symbol = "AAPL";
        var name = "Apple Inc.";
        var assetClass = "EQUITY";
        var assetSubClass = "LARGE_CAP";
        var dataSource = "Yahoo";
        var isin = "US0378331005";
        var activityCount = 10;
        var marketDataCount = 365;
        var latestPrice = "150.25";
        var latestPriceDate = DateOnly.FromDateTime(DateTime.Today);
        var totalQuantity = 100.5m;

        // Act
        var data = new HoldingPerformanceData
        {
            Symbol = symbol,
            Name = name,
            AssetClass = assetClass,
            AssetSubClass = assetSubClass,
            DataSource = dataSource,
            ISIN = isin,
            ActivityCount = activityCount,
            MarketDataCount = marketDataCount,
            LatestPrice = latestPrice,
            LatestPriceDate = latestPriceDate,
            TotalQuantity = totalQuantity
        };

        // Assert
        Assert.Equal(symbol, data.Symbol);
        Assert.Equal(name, data.Name);
        Assert.Equal(assetClass, data.AssetClass);
        Assert.Equal(assetSubClass, data.AssetSubClass);
        Assert.Equal(dataSource, data.DataSource);
        Assert.Equal(isin, data.ISIN);
        Assert.Equal(activityCount, data.ActivityCount);
        Assert.Equal(marketDataCount, data.MarketDataCount);
        Assert.Equal(latestPrice, data.LatestPrice);
        Assert.Equal(latestPriceDate, data.LatestPriceDate);
        Assert.Equal(totalQuantity, data.TotalQuantity);
    }

    [Fact]
    public void AssetClassDistributionItem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var item = new AssetClassDistributionItem();

        // Assert
        Assert.Equal(string.Empty, item.AssetClass);
        Assert.Equal(0, item.Count);
        Assert.Equal(0.0, item.Percentage);
    }

    [Fact]
    public void AssetClassDistributionItem_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var assetClass = "EQUITY";
        var count = 15;
        var percentage = 75.5;

        // Act
        var item = new AssetClassDistributionItem
        {
            AssetClass = assetClass,
            Count = count,
            Percentage = percentage
        };

        // Assert
        Assert.Equal(assetClass, item.AssetClass);
        Assert.Equal(count, item.Count);
        Assert.Equal(percentage, item.Percentage);
    }

    [Fact]
    public void ActiveHoldingItem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var item = new ActiveHoldingItem();

        // Assert
        Assert.Equal(string.Empty, item.Symbol);
        Assert.Equal(0, item.ActivityCount);
        Assert.Null(item.LatestActivityDate);
    }

    [Fact]
    public void HoldingDetailsData_ShouldInitializeWithDefaultValues()
    {
        // Act
        var data = new HoldingDetailsData();

        // Assert
        Assert.Equal(string.Empty, data.Symbol);
        Assert.NotNull(data.RecentActivities);
        Assert.Empty(data.RecentActivities);
        Assert.NotNull(data.RecentMarketData);
        Assert.Empty(data.RecentMarketData);
    }

    [Fact]
    public void ActivityItem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var item = new ActivityItem();

        // Assert
        Assert.Equal(DateTime.MinValue, item.Date);
        Assert.Equal(string.Empty, item.Type);
        Assert.Equal(string.Empty, item.Quantity);
        Assert.Equal(string.Empty, item.Price);
    }

    [Fact]
    public void MarketDataItem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var item = new MarketDataItem();

        // Assert
        Assert.Equal(DateOnly.MinValue, item.Date);
        Assert.Equal(string.Empty, item.Close);
        Assert.Equal(string.Empty, item.High);
        Assert.Equal(string.Empty, item.Low);
    }
}