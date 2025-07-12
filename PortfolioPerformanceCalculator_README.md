# Portfolio Performance Calculator with Market Data Integration

A comprehensive portfolio performance analysis system that calculates Time-Weighted Return (TWR), dividend percentages, and currency impact for investment portfolios using **real market data** for maximum accuracy.

## ?? Key Features

### ?? Market Data-Driven Calculations
- **Real-time Portfolio Valuation** using actual market prices
- **Accurate Time-Weighted Return** based on precise portfolio values
- **Historical Price Integration** for performance measurement across any time period
- **Market Data Quality Assessment** to ensure calculation reliability

### ?? Time-Weighted Return (TWR) Calculation
- Industry-standard TWR calculation using market data valuations
- Eliminates impact of cash flow timing on performance measurement
- Supports multiple periods with automatic cash flow detection
- Computes compound returns across complex portfolios

### ?? Dividend Analysis
- Tracks total dividend income across all holdings
- Calculates dividend yield based on accurate portfolio values
- Supports multiple currencies with automatic conversion
- Historical dividend performance tracking

### ?? Currency Impact Analysis
- Measures foreign currency exposure using market data
- Real-time currency conversion for accurate calculations
- Portfolio allocation analysis by currency
- Currency risk assessment and reporting

## ??? Architecture

### Core Components

#### 1. Market Data Integration
- **`MarketDataPortfolioPerformanceCalculator`** - Most accurate calculator using real market data
- **`EnhancedPortfolioPerformanceCalculator`** - Currency conversion capabilities
- **`PortfolioPerformanceCalculator`** - Basic calculator (fallback)

#### 2. Model Classes
- **`PortfolioPerformance`** - Contains all calculated performance metrics
- **`PortfolioPeriod`** - Represents time periods for TWR calculations
- **`MarketData`** - Real market price data (OHLCV)

#### 3. Services
- **`PortfolioAnalysisService`** - Intelligent service with automatic fallback
- **`PortfolioPerformanceAnalysisTask`** - Scheduled analysis with market data quality assessment

#### 4. Demo Applications
- **`MarketDataPortfolioPerformanceDemo`** - Comprehensive demo with market data
- **`PortfolioPerformanceDemo`** - Basic demo application

## ?? Usage

### Market Data-Driven Analysis (Most Accurate)
var marketDataCalculator = new MarketDataPortfolioPerformanceCalculator(
    currencyExchange, 
    logger);

var performance = await marketDataCalculator.CalculateAccuratePerformanceAsync(
    activities, 
    holdings, 
    startDate, 
    endDate, 
    Currency.EUR);

Console.WriteLine($"Accurate TWR: {performance.TimeWeightedReturn:F2}%");
Console.WriteLine($"Portfolio Value: {performance.FinalValue}");
### Intelligent Analysis Service (Automatic Fallback)
var analysisService = new PortfolioAnalysisService(
    enhancedCalculator,
    marketDataCalculator,
    logger);

// Automatically uses the most accurate method available
await analysisService.AnalyzePortfolioPerformanceAsync(
    holdings, 
    startDate, 
    endDate, 
    Currency.EUR);
### Portfolio Valuation with Market Data
var currentValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
    holdings, 
    DateTime.Now, 
    Currency.EUR);

var valuationReport = await marketDataCalculator.GenerateValuationReportAsync(
    holdings, 
    DateTime.Now, 
    Currency.EUR);
### Running the Market Data Demo
MarketDataPortfolioPerformanceDemo.RunDemo();
## ?? Calculated Metrics

### Performance Metrics (Market Data-Driven)
- **Time-Weighted Return (TWR)** - Industry-standard performance using real market valuations
- **Total Return %** - Simple return including cash flows
- **Annualized Return** - Return adjusted for time period
- **Absolute Return** - Total monetary gain/loss

### Portfolio Valuation
- **Real-time Portfolio Value** - Current market value using latest prices
- **Historical Valuations** - Portfolio value at any point in time
- **Holdings Breakdown** - Individual asset valuations
- **Market Data Coverage** - Quality assessment of available data

### Dividend Metrics
- **Total Dividends** - Sum of all dividend payments in base currency
- **Dividend Yield** - Dividends as percentage of accurate portfolio value

### Currency Metrics
- **Currency Impact** - Real impact of foreign currency exposure
- **Foreign Exposure** - Accurate percentage based on market values
- **Currency Allocation** - Portfolio breakdown by currency

## ?? Market Data Quality Assessment

The system automatically assesses market data quality:
Market Data Coverage: 95.0% (19/20 holdings)
Recent Data Coverage: 85.0% (17/20 holdings)  
Total Market Data Points: 12,847

AAPL: 365 market data points, latest: 2024-01-15 (1 days ago)
VWCE.AS: 298 market data points, latest: 2024-01-15 (1 days ago)
## ? Performance Calculation Methods

### 1. Market Data-Driven (Most Accurate)
- Uses real market prices for portfolio valuation
- Accurate TWR calculation based on precise values
- Real-time currency conversion
- **Quality:** Highest accuracy

### 2. Enhanced with Currency Conversion
- Currency conversion capabilities
- Estimated portfolio values
- **Quality:** High accuracy

### 3. Basic Calculation
- Simple calculations without external dependencies
- Placeholder values for portfolio valuation
- **Quality:** Basic accuracy

## ?? Time-Weighted Return Formula (Market Data-Enhanced)

Enhanced TWR calculation using market data:

1. **Accurate Portfolio Valuations**: Use real market prices at each period boundary
2. **Precise Cash Flow Detection**: Identify all cash flow events
3. **Sub-Period Returns**: `(End Value - Start Value - Cash Flow) / Start Value`
4. **Compound Returns**: `TWR = (1 + R?) × (1 + R?) × ... × (1 + R?) - 1`

This method provides the most accurate performance measurement possible.

## ??? Integration

### Scheduled Analysis
The `PortfolioPerformanceAnalysisTask` runs automatically:
- **Frequency**: Daily
- **Market Data Assessment**: Automatic quality checking
- **Analysis Method**: Intelligent selection based on data availability
- **Error Handling**: Graceful fallback to less accurate methods

### Background Processing
Automatic analysis includes:
- **Market Data Quality Assessment**
- **Portfolio Insights Generation**
- **Multiple Time Period Analysis**
- **Asset Allocation Analysis**
- **Individual Holding Reports**

## ?? Dependencies

- **Market Data System** - Existing `MarketDataGathererTask` integration
- **Currency Exchange Service** - Real-time currency conversion
- **Entity Framework** - Data access with market data relationships
- **Microsoft.Extensions.Logging** - Comprehensive logging

## ??? Configuration

### Base Currencyvar baseCurrency = Currency.EUR; // Configurable
### Analysis Periodsvar periods = new[]
{
    ("Last Month", now.AddMonths(-1), now),
    ("Last Quarter", now.AddMonths(-3), now),
    ("Year to Date", new DateTime(now.Year, 1, 1), now)
};
## ?? Error Handling

Robust error handling with intelligent fallback:
- **Market Data Unavailable** ? Falls back to transaction-based estimation
- **Currency Conversion Fails** ? Uses 1:1 conversion rate
- **Enhanced Calculation Fails** ? Falls back to basic calculation
- **Complete System Logs** ? Detailed logging for troubleshooting

## ?? Example Output
=== Portfolio Performance Analysis (Market Data-Driven) ===
Period: 2023-01-01 to 2024-01-01
Base Currency: EUR

=== Return Analysis ===
Time-Weighted Return: 12.50%
Initial Portfolio Value: 10,000.00 EUR (Market Data)
Final Portfolio Value: 11,750.00 EUR (Market Data)
Net Cash Flows: 500.00 EUR

=== Market Data Quality ===
Market Data Coverage: 95.0% (19/20 holdings)
Analysis Quality: Market Data-Driven (Most Accurate)

=== Portfolio Valuation Report ===
AAPL: 1,850.25 EUR (15.75% of portfolio)
VWCE.AS: 2,240.80 EUR (19.08% of portfolio)
...
Total Portfolio Value: 11,750.00 EUR
## ?? Advanced Features

- **Portfolio Allocation Analysis** by asset class and currency
- **Market Data Coverage Assessment** for quality assurance
- **Comprehensive Valuation Reports** with detailed breakdowns
- **Performance Period Comparisons** across multiple timeframes
- **Real-time Portfolio Insights** based on latest market data

## ?? Future Enhancements

- Real-time market data streaming integration
- Risk-adjusted performance metrics (Sharpe ratio, Sortino ratio)
- Benchmark comparison capabilities
- Performance attribution analysis
- Sector and geographic allocation analysis
- Monte Carlo simulation for risk analysis

This market data-driven approach ensures the highest possible accuracy in portfolio performance measurement, making it suitable for professional investment analysis and reporting.