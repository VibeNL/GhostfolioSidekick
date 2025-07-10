using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.Performance.Calculators
{
    /// <summary>
    /// Specialized calculator for risk metrics
    /// </summary>
    public static class RiskCalculator
    {
        /// <summary>
        /// Calculates Value at Risk using historical simulation method
        /// </summary>
        public static decimal CalculateHistoricalVaR(decimal[] returns, decimal confidenceLevel)
        {
            if (!returns.Any()) return 0;

            var sortedReturns = returns.OrderBy(r => r).ToArray();
            var index = (int)Math.Floor((1 - confidenceLevel) * sortedReturns.Length);
            
            return Math.Abs(sortedReturns[Math.Max(0, Math.Min(index, sortedReturns.Length - 1))]);
        }

        /// <summary>
        /// Calculates Conditional Value at Risk (Expected Shortfall)
        /// </summary>
        public static decimal CalculateConditionalVaR(decimal[] returns, decimal confidenceLevel)
        {
            if (!returns.Any()) return 0;

            var var = CalculateHistoricalVaR(returns, confidenceLevel);
            var tailReturns = returns.Where(r => r <= -var).ToArray();
            
            return tailReturns.Any() ? Math.Abs(tailReturns.Average()) : var;
        }

        /// <summary>
        /// Calculates maximum drawdown from a series of values
        /// </summary>
        public static decimal CalculateMaximumDrawdown(decimal[] values)
        {
            if (values.Length < 2) return 0;

            var maxDrawdown = 0m;
            var peak = values[0];

            foreach (var value in values.Skip(1))
            {
                if (value > peak)
                {
                    peak = value;
                }
                
                if (peak > 0)
                {
                    var drawdown = (peak - value) / peak;
                    maxDrawdown = Math.Max(maxDrawdown, drawdown);
                }
            }

            return maxDrawdown;
        }

        /// <summary>
        /// Calculates downside deviation (semi-deviation)
        /// </summary>
        public static decimal CalculateDownsideDeviation(decimal[] returns, decimal minimumAcceptableReturn = 0)
        {
            var downsideReturns = returns.Where(r => r < minimumAcceptableReturn).ToArray();
            
            if (!downsideReturns.Any()) return 0;

            var downsideVariance = downsideReturns
                .Select(r => (r - minimumAcceptableReturn) * (r - minimumAcceptableReturn))
                .Average();

            return (decimal)Math.Sqrt((double)downsideVariance);
        }

        /// <summary>
        /// Calculates Sortino ratio
        /// </summary>
        public static decimal? CalculateSortinoRatio(decimal averageReturn, decimal downsideDeviation, decimal riskFreeRate = 0)
        {
            if (downsideDeviation == 0) return null;
            return (averageReturn - riskFreeRate) / downsideDeviation;
        }

        /// <summary>
        /// Calculates Calmar ratio (annualized return / maximum drawdown)
        /// </summary>
        public static decimal? CalculateCalmarRatio(decimal annualizedReturn, decimal maxDrawdown)
        {
            if (maxDrawdown == 0) return null;
            return annualizedReturn / maxDrawdown;
        }

        /// <summary>
        /// Calculates skewness of return distribution
        /// </summary>
        public static decimal CalculateSkewness(decimal[] returns)
        {
            if (returns.Length < 3) return 0;

            var mean = returns.Average();
            var variance = returns.Select(r => (r - mean) * (r - mean)).Average();
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            if (standardDeviation == 0) return 0;

            var skewness = returns
                .Select(r => (r - mean) / standardDeviation)
                .Select(z => z * z * z)
                .Average();

            return skewness;
        }

        /// <summary>
        /// Calculates kurtosis of return distribution (excess kurtosis)
        /// </summary>
        public static decimal CalculateKurtosis(decimal[] returns)
        {
            if (returns.Length < 4) return 0;

            var mean = returns.Average();
            var variance = returns.Select(r => (r - mean) * (r - mean)).Average();
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            if (standardDeviation == 0) return 0;

            var kurtosis = returns
                .Select(r => (r - mean) / standardDeviation)
                .Select(z => z * z * z * z)
                .Average();

            return kurtosis - 3; // Excess kurtosis
        }

        /// <summary>
        /// Identifies all drawdown periods
        /// </summary>
        public static IEnumerable<(int StartIndex, int EndIndex, decimal DrawdownPercentage)> IdentifyDrawdownPeriods(decimal[] values)
        {
            var drawdowns = new List<(int StartIndex, int EndIndex, decimal DrawdownPercentage)>();
            
            if (values.Length < 2) return drawdowns;

            var peak = values[0];
            var peakIndex = 0;
            var inDrawdown = false;
            var drawdownStart = 0;

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > peak)
                {
                    // New peak
                    if (inDrawdown)
                    {
                        // End current drawdown
                        var drawdownPercentage = peak > 0 ? (peak - values[i - 1]) / peak : 0;
                        drawdowns.Add((drawdownStart, i - 1, drawdownPercentage));
                        inDrawdown = false;
                    }
                    peak = values[i];
                    peakIndex = i;
                }
                else if (values[i] < peak && !inDrawdown)
                {
                    // Start new drawdown
                    inDrawdown = true;
                    drawdownStart = peakIndex;
                }
            }

            // Handle ongoing drawdown
            if (inDrawdown)
            {
                var drawdownPercentage = peak > 0 ? (peak - values[values.Length - 1]) / peak : 0;
                drawdowns.Add((drawdownStart, values.Length - 1, drawdownPercentage));
            }

            return drawdowns;
        }

        /// <summary>
        /// Calculates rolling volatility
        /// </summary>
        public static decimal[] CalculateRollingVolatility(decimal[] returns, int windowSize)
        {
            if (returns.Length < windowSize) return [];

            var rollingVolatilities = new decimal[returns.Length - windowSize + 1];

            for (int i = 0; i <= returns.Length - windowSize; i++)
            {
                var window = returns.Skip(i).Take(windowSize).ToArray();
                var mean = window.Average();
                var variance = window.Select(r => (r - mean) * (r - mean)).Average();
                rollingVolatilities[i] = (decimal)Math.Sqrt((double)variance);
            }

            return rollingVolatilities;
        }
    }
}