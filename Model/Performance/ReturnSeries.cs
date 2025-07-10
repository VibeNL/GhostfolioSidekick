namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a series of returns for performance analysis
    /// </summary>
    public record class ReturnSeries
    {
        /// <summary>
        /// The returns in chronological order
        /// </summary>
        public IReadOnlyList<ReturnObservation> Returns { get; init; } = [];

        /// <summary>
        /// The frequency of the returns (daily, monthly, etc.)
        /// </summary>
        public ReturnFrequency Frequency { get; init; }

        /// <summary>
        /// Start date of the series
        /// </summary>
        public DateOnly StartDate { get; init; }

        /// <summary>
        /// End date of the series
        /// </summary>
        public DateOnly EndDate { get; init; }

        /// <summary>
        /// Base currency for the returns
        /// </summary>
        public Currency Currency { get; init; } = Currency.USD;

        public ReturnSeries(
            IEnumerable<ReturnObservation> returns,
            ReturnFrequency frequency,
            Currency currency)
        {
            Returns = returns.OrderBy(r => r.Date).ToList();
            Frequency = frequency;
            Currency = currency;
            
            if (Returns.Count > 0)
            {
                StartDate = Returns.First().Date;
                EndDate = Returns.Last().Date;
            }
        }

        /// <summary>
        /// Number of return observations
        /// </summary>
        public int Count => Returns.Count;

        /// <summary>
        /// Gets returns as a simple decimal array
        /// </summary>
        public decimal[] GetReturnsArray() => Returns.Select(r => r.Return).ToArray();

        /// <summary>
        /// Calculates cumulative returns
        /// </summary>
        public IEnumerable<ReturnObservation> GetCumulativeReturns()
        {
            var cumulativeReturn = 1.0m;
            
            foreach (var returnObs in Returns)
            {
                cumulativeReturn *= (1 + returnObs.Return);
                yield return new ReturnObservation(returnObs.Date, cumulativeReturn - 1);
            }
        }

        /// <summary>
        /// Gets rolling returns for a specified window
        /// </summary>
        public IEnumerable<ReturnObservation> GetRollingReturns(int windowSize)
        {
            if (windowSize <= 0 || windowSize > Returns.Count)
                yield break;

            for (var i = windowSize - 1; i < Returns.Count; i++)
            {
                var windowReturns = Returns.Skip(i - windowSize + 1).Take(windowSize);
                var cumulativeReturn = windowReturns.Aggregate(1.0m, (acc, r) => acc * (1 + r.Return)) - 1;
                
                yield return new ReturnObservation(Returns[i].Date, cumulativeReturn);
            }
        }

        /// <summary>
        /// Calculates basic statistics for the return series
        /// </summary>
        public PerformanceStatistics GetStatistics()
        {
            return new PerformanceStatistics(GetReturnsArray());
        }

        /// <summary>
        /// Annualizes a return based on the frequency
        /// </summary>
        public decimal AnnualizeReturn(decimal return_)
        {
            var periodsPerYear = Frequency switch
            {
                ReturnFrequency.Daily => 252m, // Trading days
                ReturnFrequency.Weekly => 52m,
                ReturnFrequency.Monthly => 12m,
                ReturnFrequency.Quarterly => 4m,
                ReturnFrequency.Yearly => 1m,
                _ => 1m
            };

            return (decimal)Math.Pow((double)(1 + return_), (double)periodsPerYear) - 1;
        }

        /// <summary>
        /// Annualizes volatility based on the frequency
        /// </summary>
        public decimal AnnualizeVolatility(decimal volatility)
        {
            var periodsPerYear = Frequency switch
            {
                ReturnFrequency.Daily => 252m,
                ReturnFrequency.Weekly => 52m,
                ReturnFrequency.Monthly => 12m,
                ReturnFrequency.Quarterly => 4m,
                ReturnFrequency.Yearly => 1m,
                _ => 1m
            };

            return volatility * (decimal)Math.Sqrt((double)periodsPerYear);
        }

        /// <summary>
        /// Filters returns to a specific date range
        /// </summary>
        public ReturnSeries FilterByDateRange(DateOnly startDate, DateOnly endDate)
        {
            var filteredReturns = Returns.Where(r => r.Date >= startDate && r.Date <= endDate);
            return new ReturnSeries(filteredReturns, Frequency, Currency);
        }
    }

    /// <summary>
    /// Represents a single return observation
    /// </summary>
    public record class ReturnObservation
    {
        /// <summary>
        /// Date of the observation
        /// </summary>
        public DateOnly Date { get; init; }

        /// <summary>
        /// Return value (as decimal, e.g., 0.05 for 5%)
        /// </summary>
        public decimal Return { get; init; }

        /// <summary>
        /// Portfolio value at this date (optional)
        /// </summary>
        public Money? Value { get; init; }

        public ReturnObservation(DateOnly date, decimal return_, Money? value = null)
        {
            Date = date;
            Return = return_;
            Value = value;
        }

        /// <summary>
        /// Return as percentage (e.g., 5.0 for 5%)
        /// </summary>
        public decimal ReturnPercentage => Return * 100;

        /// <summary>
        /// Whether this is a positive return
        /// </summary>
        public bool IsPositive => Return > 0;

        /// <summary>
        /// Whether this is a negative return
        /// </summary>
        public bool IsNegative => Return < 0;
    }

    /// <summary>
    /// Frequency of return calculations
    /// </summary>
    public enum ReturnFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }
}