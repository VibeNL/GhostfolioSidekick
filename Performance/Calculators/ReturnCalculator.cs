using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.Performance.Calculators
{
    /// <summary>
    /// Specialized calculator for return metrics
    /// </summary>
    public static class ReturnCalculator
    {
        /// <summary>
        /// Calculates simple return between two values
        /// </summary>
        public static decimal CalculateSimpleReturn(decimal startValue, decimal endValue)
        {
            if (startValue == 0) return 0;
            return (endValue - startValue) / startValue;
        }

        /// <summary>
        /// Calculates logarithmic return between two values
        /// </summary>
        public static decimal CalculateLogReturn(decimal startValue, decimal endValue)
        {
            if (startValue <= 0 || endValue <= 0) return 0;
            return (decimal)Math.Log((double)(endValue / startValue));
        }

        /// <summary>
        /// Calculates compound annual growth rate (CAGR)
        /// </summary>
        public static decimal CalculateCAGR(decimal startValue, decimal endValue, decimal years)
        {
            if (startValue <= 0 || years <= 0) return 0;
            return (decimal)Math.Pow((double)(endValue / startValue), 1.0 / (double)years) - 1;
        }

        /// <summary>
        /// Calculates time-weighted return from a series of sub-period returns
        /// </summary>
        public static decimal CalculateTimeWeightedReturn(decimal[] subPeriodReturns)
        {
            if (!subPeriodReturns.Any()) return 0;

            var cumulativeReturn = 1.0m;
            foreach (var periodReturn in subPeriodReturns)
            {
                cumulativeReturn *= (1 + periodReturn);
            }

            return cumulativeReturn - 1;
        }

        /// <summary>
        /// Calculates money-weighted return (IRR) using Newton-Raphson method
        /// </summary>
        public static decimal? CalculateIRR(CashFlow[] cashFlows, decimal initialValue, decimal finalValue, int maxIterations = 100, decimal tolerance = 0.0001m)
        {
            if (!cashFlows.Any()) return null;

            // Prepare cash flows for IRR calculation
            var flows = new List<(DateTime Date, decimal Amount)>
            {
                (cashFlows.First().Date.ToDateTime(TimeOnly.MinValue), -initialValue)
            };

            flows.AddRange(cashFlows.Select(cf => (cf.Date.ToDateTime(TimeOnly.MinValue), cf.Amount.Amount)));
            flows.Add((cashFlows.Last().Date.ToDateTime(TimeOnly.MinValue), finalValue));

            var startDate = flows.First().Date;
            var flowsWithDays = flows.Select(f => new
            {
                Days = (f.Date - startDate).TotalDays,
                Amount = f.Amount
            }).ToArray();

            // Newton-Raphson method
            var rate = 0.1m; // Initial guess

            for (int i = 0; i < maxIterations; i++)
            {
                var npv = 0m;
                var dnpv = 0m;

                foreach (var flow in flowsWithDays)
                {
                    var days = (decimal)flow.Days;
                    var discountFactor = (decimal)Math.Pow(1 + (double)rate, (double)(-days / 365.25));
                    
                    npv += flow.Amount * discountFactor;
                    dnpv += flow.Amount * discountFactor * (-days / 365.25) / (1 + rate);
                }

                if (Math.Abs(npv) < (double)tolerance) return rate;
                if (dnpv == 0) break;

                rate = rate - npv / dnpv;
            }

            return Math.Abs(rate) > 10 ? null : rate; // Return null for unrealistic rates
        }

        /// <summary>
        /// Calculates rolling returns for a given window size
        /// </summary>
        public static decimal[] CalculateRollingReturns(decimal[] values, int windowSize)
        {
            if (values.Length < windowSize + 1) return [];

            var rollingReturns = new decimal[values.Length - windowSize];

            for (int i = 0; i < values.Length - windowSize; i++)
            {
                var startValue = values[i];
                var endValue = values[i + windowSize];
                rollingReturns[i] = CalculateSimpleReturn(startValue, endValue);
            }

            return rollingReturns;
        }

        /// <summary>
        /// Calculates cumulative returns from a series of periodic returns
        /// </summary>
        public static decimal[] CalculateCumulativeReturns(decimal[] periodicReturns)
        {
            if (!periodicReturns.Any()) return [];

            var cumulativeReturns = new decimal[periodicReturns.Length];
            var cumulativeValue = 1.0m;

            for (int i = 0; i < periodicReturns.Length; i++)
            {
                cumulativeValue *= (1 + periodicReturns[i]);
                cumulativeReturns[i] = cumulativeValue - 1;
            }

            return cumulativeReturns;
        }

        /// <summary>
        /// Annualizes a return based on the period
        /// </summary>
        public static decimal AnnualizeReturn(decimal totalReturn, decimal days)
        {
            if (days <= 0) return 0;
            var years = days / 365.25m;
            return (decimal)Math.Pow(1 + (double)totalReturn, 1.0 / (double)years) - 1;
        }

        /// <summary>
        /// Annualizes volatility (standard deviation)
        /// </summary>
        public static decimal AnnualizeVolatility(decimal periodVolatility, ReturnFrequency frequency)
        {
            var annualizationFactor = frequency switch
            {
                ReturnFrequency.Daily => (decimal)Math.Sqrt(252), // Trading days
                ReturnFrequency.Weekly => (decimal)Math.Sqrt(52),
                ReturnFrequency.Monthly => (decimal)Math.Sqrt(12),
                ReturnFrequency.Quarterly => (decimal)Math.Sqrt(4),
                ReturnFrequency.Yearly => 1m,
                _ => 1m
            };

            return periodVolatility * annualizationFactor;
        }

        /// <summary>
        /// Calculates real return adjusted for inflation
        /// </summary>
        public static decimal CalculateRealReturn(decimal nominalReturn, decimal inflationRate)
        {
            return (1 + nominalReturn) / (1 + inflationRate) - 1;
        }

        /// <summary>
        /// Calculates excess return over a benchmark
        /// </summary>
        public static decimal CalculateExcessReturn(decimal portfolioReturn, decimal benchmarkReturn)
        {
            return portfolioReturn - benchmarkReturn;
        }

        /// <summary>
        /// Calculates geometric mean return
        /// </summary>
        public static decimal CalculateGeometricMean(decimal[] returns)
        {
            if (!returns.Any()) return 0;

            var product = 1.0m;
            foreach (var ret in returns)
            {
                product *= (1 + ret);
            }

            return (decimal)Math.Pow((double)product, 1.0 / returns.Length) - 1;
        }

        /// <summary>
        /// Calculates return attribution for different time periods
        /// </summary>
        public static Dictionary<string, decimal> CalculateReturnAttribution(
            decimal[] returns, 
            string[] periodLabels)
        {
            var attribution = new Dictionary<string, decimal>();

            if (returns.Length != periodLabels.Length) 
                return attribution;

            for (int i = 0; i < returns.Length; i++)
            {
                attribution[periodLabels[i]] = returns[i];
            }

            return attribution;
        }
    }
}