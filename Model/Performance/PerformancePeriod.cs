namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a time period for performance calculations
    /// </summary>
    public record class PerformancePeriod
    {
        /// <summary>
        /// Start date of the performance period (inclusive)
        /// </summary>
        public DateOnly StartDate { get; init; }

        /// <summary>
        /// End date of the performance period (inclusive)
        /// </summary>
        public DateOnly EndDate { get; init; }

        /// <summary>
        /// Type of period (e.g., daily, monthly, yearly, custom)
        /// </summary>
        public PerformancePeriodType PeriodType { get; init; }

        /// <summary>
        /// Optional label for the period (e.g., "Q1 2024", "January 2024")
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        /// Number of days in the period
        /// </summary>
        public int DaysInPeriod => EndDate.DayNumber - StartDate.DayNumber + 1;

        /// <summary>
        /// Whether this period includes today
        /// </summary>
        public bool IsCurrentPeriod => EndDate >= DateOnly.FromDateTime(DateTime.Today);

        public PerformancePeriod(DateOnly startDate, DateOnly endDate, PerformancePeriodType periodType, string? label = null)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date cannot be after end date");
            }

            StartDate = startDate;
            EndDate = endDate;
            PeriodType = periodType;
            Label = label;
        }

        /// <summary>
        /// Creates a period for the specified year
        /// </summary>
        public static PerformancePeriod ForYear(int year)
        {
            return new PerformancePeriod(
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31),
                PerformancePeriodType.Yearly,
                year.ToString());
        }

        /// <summary>
        /// Creates a period for the specified month
        /// </summary>
        public static PerformancePeriod ForMonth(int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            return new PerformancePeriod(
                startDate,
                endDate,
                PerformancePeriodType.Monthly,
                $"{year}-{month:D2}");
        }

        /// <summary>
        /// Creates a period for the last N days
        /// </summary>
        public static PerformancePeriod LastDays(int days)
        {
            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var startDate = endDate.AddDays(-days + 1);
            return new PerformancePeriod(
                startDate,
                endDate,
                PerformancePeriodType.Custom,
                $"Last {days} days");
        }

        /// <summary>
        /// Creates a period since inception (from start date to today)
        /// </summary>
        public static PerformancePeriod SinceInception(DateOnly inceptionDate)
        {
            return new PerformancePeriod(
                inceptionDate,
                DateOnly.FromDateTime(DateTime.Today),
                PerformancePeriodType.SinceInception,
                "Since Inception");
        }

        /// <summary>
        /// Creates a custom period
        /// </summary>
        public static PerformancePeriod Custom(DateOnly startDate, DateOnly endDate, string? label = null)
        {
            return new PerformancePeriod(startDate, endDate, PerformancePeriodType.Custom, label);
        }

        public override string ToString()
        {
            return Label ?? $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
        }
    }
}