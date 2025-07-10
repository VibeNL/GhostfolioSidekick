using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.Performance.Calculators
{
    /// <summary>
    /// Specialized calculator for benchmark comparison metrics
    /// </summary>
    public static class BenchmarkCalculator
    {
        /// <summary>
        /// Calculates alpha (excess return over benchmark adjusted for beta)
        /// </summary>
        public static decimal CalculateAlpha(decimal portfolioReturn, decimal benchmarkReturn, decimal beta, decimal riskFreeRate = 0)
        {
            var expectedReturn = riskFreeRate + beta * (benchmarkReturn - riskFreeRate);
            return portfolioReturn - expectedReturn;
        }

        /// <summary>
        /// Calculates beta (sensitivity to benchmark movements)
        /// </summary>
        public static decimal CalculateBeta(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length || portfolioReturns.Length < 2)
                return 0;

            var portfolioMean = portfolioReturns.Average();
            var benchmarkMean = benchmarkReturns.Average();

            var covariance = 0m;
            var benchmarkVariance = 0m;

            for (int i = 0; i < portfolioReturns.Length; i++)
            {
                var portfolioDeviation = portfolioReturns[i] - portfolioMean;
                var benchmarkDeviation = benchmarkReturns[i] - benchmarkMean;

                covariance += portfolioDeviation * benchmarkDeviation;
                benchmarkVariance += benchmarkDeviation * benchmarkDeviation;
            }

            if (benchmarkVariance == 0) return 0;

            return covariance / benchmarkVariance;
        }

        /// <summary>
        /// Calculates correlation coefficient between portfolio and benchmark
        /// </summary>
        public static decimal CalculateCorrelation(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length || portfolioReturns.Length < 2)
                return 0;

            var portfolioMean = portfolioReturns.Average();
            var benchmarkMean = benchmarkReturns.Average();

            var numerator = 0m;
            var portfolioSumSquares = 0m;
            var benchmarkSumSquares = 0m;

            for (int i = 0; i < portfolioReturns.Length; i++)
            {
                var portfolioDeviation = portfolioReturns[i] - portfolioMean;
                var benchmarkDeviation = benchmarkReturns[i] - benchmarkMean;

                numerator += portfolioDeviation * benchmarkDeviation;
                portfolioSumSquares += portfolioDeviation * portfolioDeviation;
                benchmarkSumSquares += benchmarkDeviation * benchmarkDeviation;
            }

            var denominator = (decimal)Math.Sqrt((double)(portfolioSumSquares * benchmarkSumSquares));
            
            if (denominator == 0) return 0;

            return numerator / denominator;
        }

        /// <summary>
        /// Calculates tracking error (standard deviation of excess returns)
        /// </summary>
        public static decimal CalculateTrackingError(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length || portfolioReturns.Length < 2)
                return 0;

            var excessReturns = portfolioReturns
                .Zip(benchmarkReturns, (p, b) => p - b)
                .ToArray();

            if (!excessReturns.Any()) return 0;

            var mean = excessReturns.Average();
            var variance = excessReturns.Select(r => (r - mean) * (r - mean)).Average();

            return (decimal)Math.Sqrt((double)variance);
        }

        /// <summary>
        /// Calculates information ratio (alpha / tracking error)
        /// </summary>
        public static decimal? CalculateInformationRatio(decimal alpha, decimal trackingError)
        {
            if (trackingError == 0) return null;
            return alpha / trackingError;
        }

        /// <summary>
        /// Calculates Treynor ratio (excess return per unit of systematic risk)
        /// </summary>
        public static decimal? CalculateTreynorRatio(decimal portfolioReturn, decimal beta, decimal riskFreeRate = 0)
        {
            if (beta == 0) return null;
            return (portfolioReturn - riskFreeRate) / beta;
        }

        /// <summary>
        /// Calculates Jensen's alpha using CAPM
        /// </summary>
        public static decimal CalculateJensensAlpha(
            decimal portfolioReturn, 
            decimal benchmarkReturn, 
            decimal riskFreeRate, 
            decimal beta)
        {
            var expectedReturn = riskFreeRate + beta * (benchmarkReturn - riskFreeRate);
            return portfolioReturn - expectedReturn;
        }

        /// <summary>
        /// Calculates upside capture ratio
        /// </summary>
        public static decimal? CalculateUpsideCapture(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length)
                return null;

            var upsidePairs = portfolioReturns
                .Zip(benchmarkReturns, (p, b) => new { Portfolio = p, Benchmark = b })
                .Where(pair => pair.Benchmark > 0)
                .ToArray();

            if (!upsidePairs.Any()) return null;

            var portfolioUpsideReturn = upsidePairs.Average(pair => pair.Portfolio);
            var benchmarkUpsideReturn = upsidePairs.Average(pair => pair.Benchmark);

            if (benchmarkUpsideReturn == 0) return null;

            return portfolioUpsideReturn / benchmarkUpsideReturn;
        }

        /// <summary>
        /// Calculates downside capture ratio
        /// </summary>
        public static decimal? CalculateDownsideCapture(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length)
                return null;

            var downsidePairs = portfolioReturns
                .Zip(benchmarkReturns, (p, b) => new { Portfolio = p, Benchmark = b })
                .Where(pair => pair.Benchmark < 0)
                .ToArray();

            if (!downsidePairs.Any()) return null;

            var portfolioDownsideReturn = downsidePairs.Average(pair => pair.Portfolio);
            var benchmarkDownsideReturn = downsidePairs.Average(pair => pair.Benchmark);

            if (benchmarkDownsideReturn == 0) return null;

            return Math.Abs(portfolioDownsideReturn) / Math.Abs(benchmarkDownsideReturn);
        }

        /// <summary>
        /// Calculates capture ratio (upside capture / downside capture)
        /// </summary>
        public static decimal? CalculateCaptureRatio(decimal[] portfolioReturns, decimal[] benchmarkReturns)
        {
            var upsideCapture = CalculateUpsideCapture(portfolioReturns, benchmarkReturns);
            var downsideCapture = CalculateDownsideCapture(portfolioReturns, benchmarkReturns);

            if (!upsideCapture.HasValue || !downsideCapture.HasValue || downsideCapture.Value == 0)
                return null;

            return upsideCapture.Value / downsideCapture.Value;
        }

        /// <summary>
        /// Calculates maximum relative drawdown vs benchmark
        /// </summary>
        public static decimal CalculateMaxRelativeDrawdown(decimal[] portfolioValues, decimal[] benchmarkValues)
        {
            if (portfolioValues.Length != benchmarkValues.Length || portfolioValues.Length < 2)
                return 0;

            var relativePerformance = new decimal[portfolioValues.Length];
            
            // Calculate relative performance (portfolio / benchmark)
            for (int i = 0; i < portfolioValues.Length; i++)
            {
                relativePerformance[i] = benchmarkValues[i] == 0 ? 0 : portfolioValues[i] / benchmarkValues[i];
            }

            return RiskCalculator.CalculateMaximumDrawdown(relativePerformance);
        }

        /// <summary>
        /// Calculates rolling correlation over time
        /// </summary>
        public static decimal[] CalculateRollingCorrelation(
            decimal[] portfolioReturns, 
            decimal[] benchmarkReturns, 
            int windowSize)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length || portfolioReturns.Length < windowSize)
                return [];

            var rollingCorrelations = new decimal[portfolioReturns.Length - windowSize + 1];

            for (int i = 0; i <= portfolioReturns.Length - windowSize; i++)
            {
                var portfolioWindow = portfolioReturns.Skip(i).Take(windowSize).ToArray();
                var benchmarkWindow = benchmarkReturns.Skip(i).Take(windowSize).ToArray();
                
                rollingCorrelations[i] = CalculateCorrelation(portfolioWindow, benchmarkWindow);
            }

            return rollingCorrelations;
        }

        /// <summary>
        /// Calculates rolling beta over time
        /// </summary>
        public static decimal[] CalculateRollingBeta(
            decimal[] portfolioReturns, 
            decimal[] benchmarkReturns, 
            int windowSize)
        {
            if (portfolioReturns.Length != benchmarkReturns.Length || portfolioReturns.Length < windowSize)
                return [];

            var rollingBetas = new decimal[portfolioReturns.Length - windowSize + 1];

            for (int i = 0; i <= portfolioReturns.Length - windowSize; i++)
            {
                var portfolioWindow = portfolioReturns.Skip(i).Take(windowSize).ToArray();
                var benchmarkWindow = benchmarkReturns.Skip(i).Take(windowSize).ToArray();
                
                rollingBetas[i] = CalculateBeta(portfolioWindow, benchmarkWindow);
            }

            return rollingBetas;
        }
    }
}