# Performance Calculation Implementation

This project contains the actual implementation of portfolio performance calculations using the domain models defined in the Model project.

## Overview

The Performance project provides:

1. **PerformanceCalculationService** - Main service implementing `IPerformanceCalculationService`
2. **Specialized Calculators** - Focused calculators for specific metric types
3. **Examples** - Comprehensive usage examples
4. **Unit Tests** - Thorough test coverage for all calculations

## Key Components

### Main Service
- **PerformanceCalculationService**: Complete implementation of the performance calculation interface

### Specialized Calculators
- **ReturnCalculator**: Returns, CAGR, IRR, time-weighted returns
- **RiskCalculator**: VaR, drawdowns, volatility, Sharpe/Sortino ratios
- **BenchmarkCalculator**: Alpha, beta, tracking error, correlation

## Implemented Calculations

### Return Metrics
? **Simple Returns**: Basic period returns  
? **Time-Weighted Returns**: Returns excluding cash flow timing impact  
? **Money-Weighted Returns (IRR)**: Returns including cash flow timing  
? **Compound Annual Growth Rate (CAGR)**: Annualized growth rate  
? **Rolling Returns**: Moving window return analysis  
? **Cumulative Returns**: Compounded returns over time  
? **Real Returns**: Inflation-adjusted returns  
? **Excess Returns**: Returns above benchmark  

### Risk Metrics
? **Volatility**: Standard deviation of returns (annualized)  
? **Value at Risk (VaR)**: Historical simulation method  
? **Conditional VaR**: Expected shortfall beyond VaR  
? **Maximum Drawdown**: Largest peak-to-trough decline  
? **Downside Deviation**: Semi-deviation of negative returns  
? **Sharpe Ratio**: Risk-adjusted return metric  
? **Sortino Ratio**: Downside risk-adjusted return  
? **Calmar Ratio**: Return per unit of drawdown risk  
? **Skewness & Kurtosis**: Distribution characteristics  

### Benchmark Analysis
? **Alpha**: Excess return vs CAPM expected return  
? **Beta**: Sensitivity to market movements  
? **Correlation**: Linear relationship strength  
? **Tracking Error**: Volatility of excess returns  
? **Information Ratio**: Alpha per unit of tracking error  
? **Treynor Ratio**: Return per unit of systematic risk  
? **Upside/Downside Capture**: Performance in up/down markets  
? **Rolling Correlation/Beta**: Time-varying relationships  

### Attribution & Allocation
? **Performance Attribution**: Holding-level contribution analysis  
? **Portfolio Allocation**: Multi-dimensional breakdowns  
? **Asset Class Analysis**: Performance by asset type  
? **Sector/Country Analysis**: Geographic/sector attribution  
? **Cash Flow Analysis**: Impact of deposits/withdrawals  

## Usage Examples

### Basic Performance Analysis
```csharp
var service = new PerformanceCalculationService(logger);
var holdings = GetPortfolioHoldings();
var period = PerformancePeriod.ForYear(2024);

var metrics = await service.CalculatePerformanceMetricsAsync(
    holdings, period, Currency.USD);

Console.WriteLine($"Total Return: {metrics.TotalReturnPercentage:P2}");
Console.WriteLine($"Sharpe Ratio: {metrics.SharpeRatio:F2}");
```

### Risk Analysis
```csharp
var returnSeries = CreateReturnSeries(snapshots);
var riskMetrics = await service.CalculateRiskMetricsAsync(returnSeries, period);

Console.WriteLine($"VaR (95%): {riskMetrics.ValueAtRisk95:P2}");
Console.WriteLine($"Max Drawdown: {riskMetrics.MaxDrawdown:P2}");
```

### Benchmark Comparison
```csharp
var comparison = await service.CalculateBenchmarkComparisonAsync(
    portfolioReturns, benchmarkReturns, benchmark, period);

Console.WriteLine($"Alpha: {comparison.Alpha:P2}");
Console.WriteLine($"Beta: {comparison.Beta:F2}");
```

### Comprehensive Reporting
```csharp
var report = await service.GeneratePerformanceReportAsync(
    holdings, period, Currency.USD, benchmarks);

var topPerformers = report.GetTopPerformers(5);
var score = report.CalculatePortfolioScore();
```

## Calculator Classes

### ReturnCalculator
Static methods for return calculations:
- `CalculateSimpleReturn()` - Basic returns
- `CalculateCAGR()` - Compound annual growth
- `CalculateIRR()` - Internal rate of return
- `CalculateTimeWeightedReturn()` - TWR from sub-periods
- `CalculateRollingReturns()` - Moving window analysis

### RiskCalculator  
Static methods for risk calculations:
- `CalculateHistoricalVaR()` - Value at Risk
- `CalculateMaximumDrawdown()` - Peak-to-trough decline
- `CalculateDownsideDeviation()` - Semi-deviation
- `CalculateSortinoRatio()` - Downside-adjusted return
- `IdentifyDrawdownPeriods()` - All drawdown events

### BenchmarkCalculator
Static methods for benchmark analysis:
- `CalculateAlpha()` - Risk-adjusted excess return
- `CalculateBeta()` - Market sensitivity
- `CalculateCorrelation()` - Linear relationship
- `CalculateTrackingError()` - Active risk
- `CalculateUpsideCapture()` - Bull market performance

## Testing

Comprehensive unit tests cover:
- ? All calculation methods
- ? Edge cases (zero values, empty arrays)
- ? Mathematical accuracy verification
- ? Error handling scenarios

Run tests with:
```bash
dotnet test Performance.UnitTests
```

## Integration

### Dependencies
- **Model Project**: Domain objects and interfaces
- **Microsoft.Extensions.Logging**: Logging infrastructure

### Usage in Applications
1. Register service in DI container
2. Inject `IPerformanceCalculationService`
3. Call calculation methods as needed

### Example DI Registration
```csharp
services.AddScoped<IPerformanceCalculationService, PerformanceCalculationService>();
```

## Implementation Notes

### Calculation Accuracy
- Uses decimal arithmetic for financial precision
- Handles edge cases (zero values, empty datasets)
- Implements standard financial formulas
- Provides reasonable defaults for missing data

### Performance Considerations
- Async/await pattern for scalability
- Efficient algorithms for large datasets
- Minimal memory allocations
- Caching strategies for repeated calculations

### Data Requirements
- Market data for valuation
- Transaction history for cash flows
- Currency exchange rates for conversions
- Benchmark data for comparisons

### Limitations
Current implementation includes simplified logic for:
- Market data integration (uses placeholders)
- Currency conversion (basic implementation)
- Complex derivatives pricing
- Real-time data feeds

Production usage would require:
- Integration with market data providers
- Robust currency conversion service
- Advanced pricing models
- Performance optimization for large portfolios

## Future Enhancements

Potential additions:
- **Advanced Risk Models**: Monte Carlo simulation, stress testing
- **Factor Analysis**: Fama-French factor attribution
- **ESG Metrics**: Sustainability performance measures
- **Tax Analysis**: After-tax return calculations
- **Sector Rotation**: Dynamic allocation analysis
- **Options Analytics**: Greeks and volatility surfaces

## Examples Project

The Examples folder contains comprehensive usage scenarios:
- Basic performance analysis
- Risk assessment workflows
- Attribution analysis patterns
- Benchmark comparison methods
- Complete reporting examples

See `Performance/Examples/PerformanceCalculationExample.cs` for detailed usage patterns.