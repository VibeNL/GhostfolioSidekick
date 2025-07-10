namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents performance statistics for a collection of holdings or time periods
    /// </summary>
    public record class PerformanceStatistics
    {
        /// <summary>
        /// Number of observations in the dataset
        /// </summary>
        public int Count { get; init; }

        /// <summary>
        /// Mean/average return
        /// </summary>
        public decimal Mean { get; init; }

        /// <summary>
        /// Median return
        /// </summary>
        public decimal Median { get; init; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public decimal StandardDeviation { get; init; }

        /// <summary>
        /// Variance
        /// </summary>
        public decimal Variance { get; init; }

        /// <summary>
        /// Minimum value
        /// </summary>
        public decimal Minimum { get; init; }

        /// <summary>
        /// Maximum value
        /// </summary>
        public decimal Maximum { get; init; }

        /// <summary>
        /// 25th percentile
        /// </summary>
        public decimal Percentile25 { get; init; }

        /// <summary>
        /// 75th percentile
        /// </summary>
        public decimal Percentile75 { get; init; }

        /// <summary>
        /// 95th percentile
        /// </summary>
        public decimal Percentile95 { get; init; }

        /// <summary>
        /// 99th percentile
        /// </summary>
        public decimal Percentile99 { get; init; }

        /// <summary>
        /// Skewness of the distribution
        /// </summary>
        public decimal Skewness { get; init; }

        /// <summary>
        /// Kurtosis of the distribution
        /// </summary>
        public decimal Kurtosis { get; init; }

        /// <summary>
        /// Sum of all values
        /// </summary>
        public decimal Sum { get; init; }

        /// <summary>
        /// Number of positive values
        /// </summary>
        public int PositiveCount { get; init; }

        /// <summary>
        /// Number of negative values
        /// </summary>
        public int NegativeCount { get; init; }

        /// <summary>
        /// Number of zero values
        /// </summary>
        public int ZeroCount { get; init; }

        public PerformanceStatistics(IEnumerable<decimal> values)
        {
            var sortedValues = values.OrderBy(x => x).ToArray();
            Count = sortedValues.Length;

            if (Count == 0)
            {
                return;
            }

            Sum = sortedValues.Sum();
            Mean = Sum / Count;
            Minimum = sortedValues[0];
            Maximum = sortedValues[Count - 1];
            
            if (Count > 1)
            {
                Median = Count % 2 == 0
                    ? (sortedValues[Count / 2 - 1] + sortedValues[Count / 2]) / 2
                    : sortedValues[Count / 2];
            }
            else
            {
                Median = sortedValues[0];
            }

            // Calculate percentiles
            Percentile25 = CalculatePercentile(sortedValues, 0.25m);
            Percentile75 = CalculatePercentile(sortedValues, 0.75m);
            Percentile95 = CalculatePercentile(sortedValues, 0.95m);
            Percentile99 = CalculatePercentile(sortedValues, 0.99m);

            // Calculate variance and standard deviation
            if (Count > 1)
            {
                Variance = sortedValues.Sum(x => (x - Mean) * (x - Mean)) / (Count - 1);
                StandardDeviation = (decimal)Math.Sqrt((double)Variance);
            }

            // Count positive, negative, and zero values
            PositiveCount = sortedValues.Count(x => x > 0);
            NegativeCount = sortedValues.Count(x => x < 0);
            ZeroCount = sortedValues.Count(x => x == 0);

            // Calculate skewness and kurtosis (simplified versions)
            if (StandardDeviation > 0 && Count > 2)
            {
                var moments = sortedValues.Select(x => (x - Mean) / StandardDeviation).ToArray();
                Skewness = moments.Sum(x => x * x * x) / Count;
                
                if (Count > 3)
                {
                    Kurtosis = moments.Sum(x => x * x * x * x) / Count - 3; // Excess kurtosis
                }
            }
        }

        private static decimal CalculatePercentile(decimal[] sortedValues, decimal percentile)
        {
            if (sortedValues.Length == 0) return 0;
            if (sortedValues.Length == 1) return sortedValues[0];

            var index = percentile * (sortedValues.Length - 1);
            var lower = (int)Math.Floor((double)index);
            var upper = (int)Math.Ceiling((double)index);

            if (lower == upper)
            {
                return sortedValues[lower];
            }

            var weight = index - lower;
            return sortedValues[lower] * (1 - (decimal)weight) + sortedValues[upper] * (decimal)weight;
        }

        /// <summary>
        /// Range (difference between max and min)
        /// </summary>
        public decimal Range => Maximum - Minimum;

        /// <summary>
        /// Interquartile range (75th - 25th percentile)
        /// </summary>
        public decimal InterquartileRange => Percentile75 - Percentile25;

        /// <summary>
        /// Percentage of positive values
        /// </summary>
        public decimal PositivePercentage => Count == 0 ? 0 : (decimal)PositiveCount / Count;

        /// <summary>
        /// Win/loss ratio (positive count / negative count)
        /// </summary>
        public decimal? WinLossRatio => NegativeCount == 0 ? null : (decimal)PositiveCount / NegativeCount;
    }
}