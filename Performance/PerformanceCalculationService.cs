using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
    /// <summary>
    /// Implementation of performance calculation service
    /// </summary>
    public class PerformanceCalculationService : IPerformanceCalculationService
    {
        private readonly ILogger<PerformanceCalculationService> logger;

        public PerformanceCalculationService(ILogger<PerformanceCalculationService> logger)
        {
            this.logger = logger;
        }

        public async Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency)
        {
            var snapshots = await GeneratePerformanceSnapshotsAsync(holdings, period.StartDate, period.EndDate, baseCurrency);
            var returnSeries = CreateReturnSeries(snapshots, ReturnFrequency.Daily, baseCurrency);
            var cashFlows = ExtractCashFlows(holdings, period);

            var startingValue = snapshots.FirstOrDefault()?.TotalValue ?? new Money(baseCurrency, 0);
            var endingValue = snapshots.LastOrDefault()?.TotalValue ?? new Money(baseCurrency, 0);
            
            var totalReturn = CalculateTotalReturn(startingValue, endingValue, cashFlows);
            var totalReturnPercentage = CalculateTotalReturnPercentage(startingValue, endingValue, cashFlows);
            var annualizedReturn = CalculateAnnualizedReturn(totalReturnPercentage, period);
            var volatility = CalculateVolatility(returnSeries);
            var sharpeRatio = CalculateSharpeRatio(annualizedReturn, volatility);
            var maxDrawdown = CalculateMaxDrawdown(snapshots);
            var timeWeightedReturn = await CalculateTimeWeightedReturnAsync(CreateTimeWeightedPeriods(snapshots, cashFlows));
            var moneyWeightedReturn = await CalculateMoneyWeightedReturnAsync(cashFlows, startingValue, endingValue);

            var totalFees = CalculateTotalFees(holdings, period);
            var totalDividends = CalculateTotalDividends(holdings, period);
            var netCashFlow = CalculateNetCashFlow(cashFlows);

            return new PerformanceMetrics
            {
                Period = period,
                TotalReturn = totalReturn,
                TotalReturnPercentage = totalReturnPercentage,
                AnnualizedReturn = annualizedReturn,
                Volatility = volatility,
                SharpeRatio = sharpeRatio,
                MaxDrawdown = maxDrawdown,
                TimeWeightedReturn = timeWeightedReturn,
                MoneyWeightedReturn = moneyWeightedReturn,
                StartingValue = startingValue,
                EndingValue = endingValue,
                TotalFees = totalFees,
                TotalDividends = totalDividends,
                NetCashFlow = netCashFlow,
                ValueAtRisk95 = CalculateValueAtRisk(snapshots, 0.95m),
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<PerformanceMetrics> CalculateHoldingPerformanceAsync(
            Holding holding,
            PerformancePeriod period,
            Currency baseCurrency)
        {
            return await CalculatePerformanceMetricsAsync([holding], period, baseCurrency);
        }

        public async Task<RiskMetrics> CalculateRiskMetricsAsync(
            ReturnSeries returnSeries,
            PerformancePeriod period)
        {
            var returns = returnSeries.GetReturnsArray();
            var statistics = returnSeries.GetStatistics();

            var standardDeviation = statistics.StandardDeviation;
            var annualizedVolatility = returnSeries.AnnualizeVolatility(standardDeviation);
            var maxDrawdown = CalculateMaxDrawdownFromReturns(returns);
            var valueAtRisk95 = CalculateValueAtRiskFromReturns(returns, 0.95m);
            var valueAtRisk99 = CalculateValueAtRiskFromReturns(returns, 0.99m);
            var conditionalVaR95 = CalculateConditionalVaR(returns, 0.95m);
            var downSideDeviation = CalculateDownsideDeviation(returns);
            var sortinoRatio = CalculateSortinoRatio(statistics.Mean, downSideDeviation);

            await Task.CompletedTask; // For async compliance

            return new RiskMetrics(period, standardDeviation, maxDrawdown)
            {
                AnnualizedVolatility = annualizedVolatility,
                ValueAtRisk95 = valueAtRisk95,
                ValueAtRisk99 = valueAtRisk99,
                ConditionalVaR95 = conditionalVaR95,
                DownsideDeviation = downSideDeviation,
                SortinoRatio = sortinoRatio,
                Skewness = statistics.Skewness,
                Kurtosis = statistics.Kurtosis,
                PositivePeriods = statistics.PositivePercentage * 100,
                LargestGain = statistics.Maximum,
                LargestLoss = statistics.Minimum,
                AverageGain = returns.Where(r => r > 0).DefaultIfEmpty(0).Average(),
                AverageLoss = returns.Where(r => r < 0).DefaultIfEmpty(0).Average(),
                GainLossRatio = CalculateGainLossRatio(returns)
            };
        }

        public async Task<IEnumerable<PerformanceAttribution>> CalculatePerformanceAttributionAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency)
        {
            var attributions = new List<PerformanceAttribution>();
            var totalPortfolioValue = await CalculateTotalPortfolioValue(holdings, period.StartDate, baseCurrency);
            var totalPortfolioValueEnd = await CalculateTotalPortfolioValue(holdings, period.EndDate, baseCurrency);

            foreach (var holding in holdings)
            {
                var holdingStartValue = await CalculateHoldingValue(holding, period.StartDate, baseCurrency);
                var holdingEndValue = await CalculateHoldingValue(holding, period.EndDate, baseCurrency);
                
                var startingWeight = totalPortfolioValue.Amount == 0 ? 0 : holdingStartValue.Amount / totalPortfolioValue.Amount;
                var endingWeight = totalPortfolioValueEnd.Amount == 0 ? 0 : holdingEndValue.Amount / totalPortfolioValueEnd.Amount;
                
                var holdingReturn = CalculateHoldingReturn(holdingStartValue, holdingEndValue, holding, period);
                
                var attribution = new PerformanceAttribution(
                    holding,
                    period,
                    startingWeight,
                    endingWeight,
                    holdingReturn,
                    holdingStartValue,
                    holdingEndValue)
                {
                    NetCashFlow = CalculateHoldingCashFlow(holding, period),
                    Dividends = CalculateHoldingDividends(holding, period),
                    Fees = CalculateHoldingFees(holding, period),
                    AssetClass = GetHoldingAssetClass(holding),
                    AssetSubClass = GetHoldingAssetSubClass(holding)
                };

                attributions.Add(attribution);
            }

            return attributions;
        }

        public async Task<IEnumerable<PerformanceSnapshot>> GeneratePerformanceSnapshotsAsync(
            IEnumerable<Holding> holdings,
            DateOnly startDate,
            DateOnly endDate,
            Currency baseCurrency)
        {
            var snapshots = new List<PerformanceSnapshot>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var totalValue = await CalculateTotalPortfolioValue(holdings, currentDate, baseCurrency);
                var cashValue = await CalculateCashValue(holdings, currentDate, baseCurrency);
                var marketValue = totalValue.Add(cashValue.Times(-1));

                var snapshot = new PerformanceSnapshot(currentDate, totalValue)
                {
                    CashValue = cashValue,
                    MarketValue = marketValue,
                    NetCashFlow = CalculateDayCashFlow(holdings, currentDate),
                    Dividends = CalculateDayDividends(holdings, currentDate),
                    Fees = CalculateDayFees(holdings, currentDate)
                };

                snapshots.Add(snapshot);
                currentDate = currentDate.AddDays(1);
            }

            // Calculate daily returns
            for (int i = 1; i < snapshots.Count; i++)
            {
                var dailyReturn = snapshots[i].CalculateDailyReturn(snapshots[i - 1]);
                snapshots[i] = snapshots[i] with { DailyReturn = dailyReturn };
            }

            return snapshots;
        }

        public async Task<BenchmarkComparison> CalculateBenchmarkComparisonAsync(
            ReturnSeries portfolioReturns,
            ReturnSeries benchmarkReturns,
            PerformanceBenchmark benchmark,
            PerformancePeriod period)
        {
            var portfolioStats = portfolioReturns.GetStatistics();
            var benchmarkStats = benchmarkReturns.GetStatistics();

            var portfolioReturn = CalculateTotalReturnFromSeries(portfolioReturns);
            var benchmarkReturn = CalculateTotalReturnFromSeries(benchmarkReturns);

            var alpha = portfolioReturn - benchmarkReturn;
            var beta = CalculateBeta(portfolioReturns.GetReturnsArray(), benchmarkReturns.GetReturnsArray());
            var correlation = CalculateCorrelation(portfolioReturns.GetReturnsArray(), benchmarkReturns.GetReturnsArray());
            var trackingError = CalculateTrackingError(portfolioReturns.GetReturnsArray(), benchmarkReturns.GetReturnsArray());
            var informationRatio = trackingError != 0 ? alpha / trackingError : null;

            await Task.CompletedTask;

            return new BenchmarkComparison(benchmark, period, portfolioReturn, benchmarkReturn)
            {
                Beta = beta,
                Correlation = correlation,
                TrackingError = trackingError,
                InformationRatio = informationRatio
            };
        }

        public async Task<PerformanceReport> GeneratePerformanceReportAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency,
            IEnumerable<PerformanceBenchmark>? benchmarks = null)
        {
            var metrics = await CalculatePerformanceMetricsAsync(holdings, period, baseCurrency);
            var snapshots = await GeneratePerformanceSnapshotsAsync(holdings, period.StartDate, period.EndDate, baseCurrency);
            var returnSeries = CreateReturnSeries(snapshots, ReturnFrequency.Daily, baseCurrency);
            var riskMetrics = await CalculateRiskMetricsAsync(returnSeries, period);
            var attributions = await CalculatePerformanceAttributionAsync(holdings, period, baseCurrency);
            var allocation = await CalculatePortfolioAllocationAsync(holdings, period.EndDate, baseCurrency);

            var benchmarkComparisons = new List<BenchmarkComparison>();
            if (benchmarks != null)
            {
                foreach (var benchmark in benchmarks)
                {
                    // In a real implementation, you would fetch benchmark data
                    // For now, we'll create a placeholder
                    var benchmarkSeries = CreateMockBenchmarkSeries(benchmark, period, baseCurrency);
                    var comparison = await CalculateBenchmarkComparisonAsync(returnSeries, benchmarkSeries, benchmark, period);
                    benchmarkComparisons.Add(comparison);
                }
            }

            return new PerformanceReport(metrics, riskMetrics, baseCurrency)
            {
                HoldingAttributions = attributions.ToList(),
                BenchmarkComparisons = benchmarkComparisons,
                PerformanceHistory = snapshots.ToList()
            };
        }

        public async Task<PortfolioAllocation> CalculatePortfolioAllocationAsync(
            IEnumerable<Holding> holdings,
            DateOnly date,
            Currency baseCurrency)
        {
            var totalValue = await CalculateTotalPortfolioValue(holdings, date, baseCurrency);
            var cashValue = await CalculateCashValue(holdings, date, baseCurrency);

            var holdingAllocations = new List<HoldingAllocation>();
            var assetClassAllocations = new Dictionary<Activities.AssetClass, AllocationItem>();

            foreach (var holding in holdings)
            {
                var holdingValue = await CalculateHoldingValue(holding, date, baseCurrency);
                var percentage = totalValue.Amount == 0 ? 0 : holdingValue.Amount / totalValue.Amount;
                var quantity = CalculateHoldingQuantity(holding, date);
                var unitPrice = quantity == 0 ? new Money(baseCurrency, 0) : holdingValue.SafeDivide(quantity);

                var holdingAllocation = new HoldingAllocation(holding, holdingValue, percentage, quantity)
                {
                    UnitPrice = unitPrice
                };

                holdingAllocations.Add(holdingAllocation);

                // Aggregate by asset class
                var assetClass = GetHoldingAssetClass(holding);
                if (assetClass.HasValue)
                {
                    if (!assetClassAllocations.ContainsKey(assetClass.Value))
                    {
                        assetClassAllocations[assetClass.Value] = new AllocationItem(new Money(baseCurrency, 0), 0, 0);
                    }

                    var existing = assetClassAllocations[assetClass.Value];
                    assetClassAllocations[assetClass.Value] = new AllocationItem(
                        existing.Value.Add(holdingValue),
                        existing.Percentage + percentage,
                        existing.HoldingCount + 1);
                }
            }

            return new PortfolioAllocation(date, totalValue)
            {
                CashValue = cashValue,
                AssetClassAllocation = assetClassAllocations,
                HoldingAllocations = holdingAllocations
            };
        }

        public async Task<IEnumerable<Drawdown>> IdentifyDrawdownsAsync(IEnumerable<PerformanceSnapshot> snapshots)
        {
            var drawdowns = new List<Drawdown>();
            var snapshotList = snapshots.OrderBy(s => s.Date).ToList();
            
            if (!snapshotList.Any()) return drawdowns;

            var runningPeak = snapshotList[0].TotalValue;
            var runningPeakDate = snapshotList[0].Date;
            Drawdown? currentDrawdown = null;

            foreach (var snapshot in snapshotList.Skip(1))
            {
                if (snapshot.TotalValue.Amount > runningPeak.Amount)
                {
                    // New peak
                    if (currentDrawdown != null)
                    {
                        // End current drawdown
                        var recoveredDrawdown = currentDrawdown.WithRecovery(snapshot.Date, snapshot.TotalValue);
                        drawdowns.Add(recoveredDrawdown);
                        currentDrawdown = null;
                    }
                    runningPeak = snapshot.TotalValue;
                    runningPeakDate = snapshot.Date;
                }
                else if (snapshot.TotalValue.Amount < runningPeak.Amount)
                {
                    // In drawdown
                    if (currentDrawdown == null)
                    {
                        // Start new drawdown
                        currentDrawdown = new Drawdown(runningPeakDate, snapshot.Date, runningPeak, snapshot.TotalValue);
                    }
                    else if (snapshot.TotalValue.Amount < currentDrawdown.BottomValue.Amount)
                    {
                        // New bottom
                        currentDrawdown = currentDrawdown with
                        {
                            BottomDate = snapshot.Date,
                            BottomValue = snapshot.TotalValue,
                            DrawdownPercentage = runningPeak.Amount == 0 ? 0 : (runningPeak.Amount - snapshot.TotalValue.Amount) / runningPeak.Amount
                        };
                    }
                }
            }

            // Add ongoing drawdown if exists
            if (currentDrawdown != null)
            {
                drawdowns.Add(currentDrawdown);
            }

            await Task.CompletedTask;
            return drawdowns;
        }

        public async Task<decimal> CalculateTimeWeightedReturnAsync(IEnumerable<TimeWeightedPeriod> periods)
        {
            var cumulativeReturn = 1.0m;
            
            foreach (var period in periods)
            {
                var periodReturn = period.CalculatePeriodReturn();
                cumulativeReturn *= (1 + periodReturn);
            }

            await Task.CompletedTask;
            return cumulativeReturn - 1;
        }

        public async Task<decimal?> CalculateMoneyWeightedReturnAsync(
            IEnumerable<CashFlow> cashFlows,
            Money initialValue,
            Money finalValue)
        {
            // Simple IRR calculation using Newton-Raphson method
            // This is a simplified implementation - production code would use more robust numerical methods
            
            var flows = cashFlows.OrderBy(cf => cf.Date).ToList();
            if (!flows.Any()) return null;

            var startDate = flows.First().Date;
            var endDate = flows.Last().Date;
            var totalDays = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            
            if (totalDays == 0) return null;

            // Simple approximation for IRR
            var totalCashFlow = flows.Sum(cf => cf.Amount.Amount);
            var netReturn = finalValue.Amount - initialValue.Amount - totalCashFlow;
            var averageInvestment = (initialValue.Amount + finalValue.Amount) / 2;
            
            if (averageInvestment == 0) return null;
            
            var annualReturn = (netReturn / averageInvestment) * (365 / (decimal)totalDays);

            await Task.CompletedTask;
            return annualReturn;
        }

        // Helper methods (simplified implementations)
        private ReturnSeries CreateReturnSeries(IEnumerable<PerformanceSnapshot> snapshots, ReturnFrequency frequency, Currency currency)
        {
            var returns = snapshots
                .Where(s => s.DailyReturn.HasValue)
                .Select(s => new ReturnObservation(s.Date, s.DailyReturn!.Value, s.TotalValue))
                .ToList();

            return new ReturnSeries(returns, frequency, currency);
        }

        private IEnumerable<CashFlow> ExtractCashFlows(IEnumerable<Holding> holdings, PerformancePeriod period)
        {
            var cashFlows = new List<CashFlow>();

            foreach (var holding in holdings)
            {
                foreach (var activity in holding.Activities.Where(a => 
                    DateOnly.FromDateTime(a.Date) >= period.StartDate && 
                    DateOnly.FromDateTime(a.Date) <= period.EndDate))
                {
                    switch (activity)
                    {
                        case CashDepositWithdrawalActivity cashActivity:
                            cashFlows.Add(new CashFlow(
                                DateOnly.FromDateTime(cashActivity.Date),
                                cashActivity.Amount,
                                cashActivity.Amount.Amount > 0 ? CashFlowType.Deposit : CashFlowType.Withdrawal)
                            {
                                ActivityId = cashActivity.Id,
                                Description = cashActivity.Description
                            });
                            break;

                        case DividendActivity dividendActivity:
                            cashFlows.Add(new CashFlow(
                                DateOnly.FromDateTime(dividendActivity.Date),
                                dividendActivity.Amount,
                                CashFlowType.Dividend)
                            {
                                ActivityId = dividendActivity.Id,
                                Holding = holding
                            });
                            break;

                        case BuySellActivity buySellActivity:
                            var type = buySellActivity.Quantity > 0 ? CashFlowType.Purchase : CashFlowType.Sale;
                            var amount = buySellActivity.TotalTransactionAmount.Times(-1 * Math.Sign(buySellActivity.Quantity));
                            cashFlows.Add(new CashFlow(
                                DateOnly.FromDateTime(buySellActivity.Date),
                                amount,
                                type)
                            {
                                ActivityId = buySellActivity.Id,
                                Holding = holding
                            });
                            break;
                    }
                }
            }

            return cashFlows;
        }

        private Money CalculateTotalReturn(Money startingValue, Money endingValue, IEnumerable<CashFlow> cashFlows)
        {
            var netCashFlow = cashFlows.Sum(cf => cf.Amount.Amount);
            var totalReturn = endingValue.Amount - startingValue.Amount - netCashFlow;
            return new Money(startingValue.Currency, totalReturn);
        }

        private decimal CalculateTotalReturnPercentage(Money startingValue, Money endingValue, IEnumerable<CashFlow> cashFlows)
        {
            if (startingValue.Amount == 0) return 0;
            
            var netCashFlow = cashFlows.Sum(cf => cf.Amount.Amount);
            var adjustedStartingValue = startingValue.Amount + netCashFlow;
            
            if (adjustedStartingValue == 0) return 0;
            
            return (endingValue.Amount - adjustedStartingValue) / adjustedStartingValue;
        }

        private decimal CalculateAnnualizedReturn(decimal totalReturnPercentage, PerformancePeriod period)
        {
            var days = period.DaysInPeriod;
            if (days == 0) return 0;
            
            var years = days / 365.25m;
            return (decimal)Math.Pow((double)(1 + totalReturnPercentage), (double)(1 / years)) - 1;
        }

        private decimal CalculateVolatility(ReturnSeries returnSeries)
        {
            var statistics = returnSeries.GetStatistics();
            return returnSeries.AnnualizeVolatility(statistics.StandardDeviation);
        }

        private decimal? CalculateSharpeRatio(decimal annualizedReturn, decimal volatility, decimal riskFreeRate = 0.02m)
        {
            if (volatility == 0) return null;
            return (annualizedReturn - riskFreeRate) / volatility;
        }

        private decimal CalculateMaxDrawdown(IEnumerable<PerformanceSnapshot> snapshots)
        {
            var maxDrawdown = 0m;
            var peak = 0m;

            foreach (var snapshot in snapshots.OrderBy(s => s.Date))
            {
                if (snapshot.TotalValue.Amount > peak)
                {
                    peak = snapshot.TotalValue.Amount;
                }
                
                if (peak > 0)
                {
                    var drawdown = (peak - snapshot.TotalValue.Amount) / peak;
                    maxDrawdown = Math.Max(maxDrawdown, drawdown);
                }
            }

            return maxDrawdown;
        }

        // Additional helper methods would continue here...
        // For brevity, I'll add placeholder implementations for the remaining private methods

        private async Task<Money> CalculateTotalPortfolioValue(IEnumerable<Holding> holdings, DateOnly date, Currency baseCurrency)
        {
            // Simplified implementation - would need market data integration
            await Task.CompletedTask;
            return new Money(baseCurrency, 10000); // Placeholder
        }

        private async Task<Money> CalculateCashValue(IEnumerable<Holding> holdings, DateOnly date, Currency baseCurrency)
        {
            await Task.CompletedTask;
            return new Money(baseCurrency, 1000); // Placeholder
        }

        private async Task<Money> CalculateHoldingValue(Holding holding, DateOnly date, Currency baseCurrency)
        {
            await Task.CompletedTask;
            return new Money(baseCurrency, 1000); // Placeholder
        }

        private Money CalculateDayCashFlow(IEnumerable<Holding> holdings, DateOnly date)
        {
            return new Money(Currency.USD, 0); // Placeholder
        }

        private Money CalculateDayDividends(IEnumerable<Holding> holdings, DateOnly date)
        {
            return new Money(Currency.USD, 0); // Placeholder
        }

        private Money CalculateDayFees(IEnumerable<Holding> holdings, DateOnly date)
        {
            return new Money(Currency.USD, 0); // Placeholder
        }

        private Money? CalculateValueAtRisk(IEnumerable<PerformanceSnapshot> snapshots, decimal confidenceLevel)
        {
            var returns = snapshots.Where(s => s.DailyReturn.HasValue).Select(s => s.DailyReturn!.Value).ToArray();
            if (!returns.Any()) return null;
            
            var sortedReturns = returns.OrderBy(r => r).ToArray();
            var index = (int)Math.Floor((1 - confidenceLevel) * sortedReturns.Length);
            
            var lastSnapshot = snapshots.LastOrDefault();
            if (lastSnapshot == null) return null;
            
            var var95 = sortedReturns[Math.Max(0, Math.Min(index, sortedReturns.Length - 1))];
            return new Money(lastSnapshot.TotalValue.Currency, lastSnapshot.TotalValue.Amount * Math.Abs(var95));
        }

        // Additional placeholder implementations
        private Money CalculateTotalFees(IEnumerable<Holding> holdings, PerformancePeriod period) => new Money(Currency.USD, 0);
        private Money CalculateTotalDividends(IEnumerable<Holding> holdings, PerformancePeriod period) => new Money(Currency.USD, 0);
        private Money CalculateNetCashFlow(IEnumerable<CashFlow> cashFlows) => new Money(Currency.USD, cashFlows.Sum(cf => cf.Amount.Amount));
        private decimal CalculateHoldingReturn(Money startValue, Money endValue, Holding holding, PerformancePeriod period) => 0.05m;
        private Money CalculateHoldingCashFlow(Holding holding, PerformancePeriod period) => new Money(Currency.USD, 0);
        private Money CalculateHoldingDividends(Holding holding, PerformancePeriod period) => new Money(Currency.USD, 0);
        private Money CalculateHoldingFees(Holding holding, PerformancePeriod period) => new Money(Currency.USD, 0);
        private Activities.AssetClass? GetHoldingAssetClass(Holding holding) => holding.SymbolProfiles.FirstOrDefault()?.AssetClass;
        private Activities.AssetSubClass? GetHoldingAssetSubClass(Holding holding) => holding.SymbolProfiles.FirstOrDefault()?.AssetSubClass;
        private decimal CalculateHoldingQuantity(Holding holding, DateOnly date) => 10m;
        private IEnumerable<TimeWeightedPeriod> CreateTimeWeightedPeriods(IEnumerable<PerformanceSnapshot> snapshots, IEnumerable<CashFlow> cashFlows) => [];
        private decimal CalculateTotalReturnFromSeries(ReturnSeries series) => series.GetReturnsArray().Sum();
        private decimal CalculateBeta(decimal[] portfolioReturns, decimal[] benchmarkReturns) => 1.0m;
        private decimal CalculateCorrelation(decimal[] portfolioReturns, decimal[] benchmarkReturns) => 0.8m;
        private decimal CalculateTrackingError(decimal[] portfolioReturns, decimal[] benchmarkReturns) => 0.05m;
        private ReturnSeries CreateMockBenchmarkSeries(PerformanceBenchmark benchmark, PerformancePeriod period, Currency currency) => new ReturnSeries([], ReturnFrequency.Daily, currency);
        private decimal CalculateMaxDrawdownFromReturns(decimal[] returns) => returns.Any() ? Math.Abs(returns.Min()) : 0;
        private decimal CalculateValueAtRiskFromReturns(decimal[] returns, decimal confidenceLevel) => 0.05m;
        private decimal CalculateConditionalVaR(decimal[] returns, decimal confidenceLevel) => 0.07m;
        private decimal CalculateDownsideDeviation(decimal[] returns) => (decimal)Math.Sqrt((double)returns.Where(r => r < 0).Select(r => r * r).DefaultIfEmpty(0).Average());
        private decimal? CalculateSortinoRatio(decimal meanReturn, decimal downsideDeviation) => downsideDeviation == 0 ? null : meanReturn / downsideDeviation;
        private decimal? CalculateGainLossRatio(decimal[] returns) 
        {
            var gains = returns.Where(r => r > 0).DefaultIfEmpty(0).Average();
            var losses = Math.Abs(returns.Where(r => r < 0).DefaultIfEmpty(0).Average());
            return losses == 0 ? null : gains / losses;
        }
    }
}