# Performance Domain Models

This document provides an overview of the domain objects created for portfolio performance metrics calculation.

## Core Domain Objects

### 1. PerformanceMetrics
The main domain object that contains calculated performance metrics for a portfolio, account, or holding.

**Key Properties:**
- `TotalReturn` - Absolute return in money terms
- `TotalReturnPercentage` - Return as a percentage
- `AnnualizedReturn` - Annualized return percentage
- `Volatility` - Standard deviation of returns
- `SharpeRatio` - Risk-adjusted return metric
- `MaxDrawdown` - Largest peak-to-trough decline
- `TimeWeightedReturn` - Performance excluding impact of cash flows
- `MoneyWeightedReturn` - IRR including impact of cash flows timing

### 2. PerformancePeriod
Value object representing time periods for performance calculations.

**Key Features:**
- Factory methods for common periods (yearly, monthly, since inception)
- Support for custom date ranges
- Automatic calculation of period length
- Current period detection

### 3. PerformanceSnapshot
Represents portfolio value at a specific point in time.

**Key Properties:**
- `Date` - Snapshot date
- `TotalValue` - Portfolio value at this date
- `CashValue` - Cash position
- `MarketValue` - Market value of holdings
- `NetCashFlow` - Cash flows on this date
- `DailyReturn` - Return compared to previous day

### 4. RiskMetrics
Comprehensive risk analysis for portfolio.

**Key Properties:**
- `StandardDeviation` - Volatility measure
- `ValueAtRisk95/99` - Potential losses at confidence levels
- `MaxDrawdown` - Largest decline from peak
- `SortinoRatio` - Downside deviation adjusted return
- `Skewness/Kurtosis` - Distribution characteristics

### 5. PerformanceBenchmark
Represents benchmarks for performance comparison.

**Features:**
- Predefined common benchmarks (S&P 500, NASDAQ, etc.)
- Support for custom benchmarks
- Data source configuration

### 6. BenchmarkComparison
Compares portfolio performance against benchmarks.

**Key Metrics:**
- `Alpha` - Excess return over benchmark
- `Beta` - Sensitivity to benchmark movements
- `Correlation` - Relationship strength
- `TrackingError` - Volatility of excess returns
- `InformationRatio` - Risk-adjusted alpha

### 7. PerformanceAttribution
Shows how individual holdings contribute to overall performance.

**Key Properties:**
- `HoldingReturn` - Individual holding performance
- `ContributionToReturn` - Contribution to portfolio return
- `StartingWeight/EndingWeight` - Position sizing
- `AbsoluteContribution` - Money contribution

### 8. PortfolioAllocation
Breakdown of portfolio allocation across dimensions.

**Allocation Types:**
- Asset class allocation
- Currency allocation
- Sector/country allocation
- Individual holding allocation

## Supporting Value Objects

### 9. CashFlow
Represents individual cash flow events.

**Types Supported:**
- Deposits/Withdrawals
- Dividends/Interest
- Fees/Taxes
- Purchases/Sales
- Transfers

### 10. Drawdown
Represents decline periods in portfolio value.

**Key Properties:**
- Peak value before decline
- Bottom value during decline
- Recovery information
- Duration metrics

### 11. ReturnSeries
Time series of returns for analysis.

**Features:**
- Multiple frequencies (daily, monthly, etc.)
- Cumulative return calculation
- Rolling return analysis
- Statistical analysis integration

### 12. PerformanceStatistics
Comprehensive statistical analysis of return data.

**Metrics Included:**
- Mean, median, standard deviation
- Percentiles (25th, 75th, 95th, 99th)
- Skewness and kurtosis
- Win/loss ratios

### 13. TimeWeightedPeriod
Sub-periods for time-weighted return calculations.

**Purpose:**
- Break down performance into cash flow periods
- Calculate period-specific returns
- Support precise time-weighted calculations

### 14. PerformanceReport
Comprehensive container for all performance analysis.

**Contains:**
- Basic performance metrics
- Risk analysis
- Attribution analysis
- Benchmark comparisons
- Historical data

## Service Interface

### IPerformanceCalculationService
Domain service interface defining contracts for:
- Performance metric calculations
- Risk analysis
- Attribution analysis
- Benchmark comparisons
- Report generation

## Usage Patterns

These domain objects support various performance analysis scenarios:

1. **Basic Performance Analysis**: Using `PerformanceMetrics` and `PerformancePeriod`
2. **Risk Assessment**: Using `RiskMetrics` and `Drawdown` objects
3. **Benchmark Analysis**: Using `BenchmarkComparison` and `PerformanceBenchmark`
4. **Attribution Analysis**: Using `PerformanceAttribution` for holdings breakdown
5. **Comprehensive Reporting**: Using `PerformanceReport` for complete analysis

## Design Principles

- **Immutable Value Objects**: Most objects are records with init-only properties
- **Rich Domain Model**: Objects contain behavior relevant to their data
- **Separation of Concerns**: Clear separation between data and calculation logic
- **Type Safety**: Strong typing with enumerations for categories
- **Testability**: Pure functions and immutable objects for easy testing

## Future Extensions

The domain model supports extension for:
- Additional risk metrics (CVaR, Ulcer Index, etc.)
- More sophisticated attribution models
- Custom benchmark definitions
- Multi-currency performance analysis
- ESG/sustainability metrics