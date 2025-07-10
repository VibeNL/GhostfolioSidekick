namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a time-weighted return calculation period
    /// Used for breaking down performance calculations into sub-periods
    /// </summary>
    public record class TimeWeightedPeriod
    {
        /// <summary>
        /// Start date of this sub-period
        /// </summary>
        public DateOnly StartDate { get; init; }

        /// <summary>
        /// End date of this sub-period
        /// </summary>
        public DateOnly EndDate { get; init; }

        /// <summary>
        /// Starting value for this period
        /// </summary>
        public Money StartingValue { get; init; } = new Money();

        /// <summary>
        /// Ending value for this period
        /// </summary>
        public Money EndingValue { get; init; } = new Money();

        /// <summary>
        /// Cash flows during this period
        /// </summary>
        public IReadOnlyList<CashFlow> CashFlows { get; init; } = [];

        /// <summary>
        /// Return for this specific period
        /// </summary>
        public decimal PeriodReturn { get; init; }

        /// <summary>
        /// Weight of this period in the overall calculation
        /// </summary>
        public decimal Weight { get; init; }

        public TimeWeightedPeriod(
            DateOnly startDate,
            DateOnly endDate,
            Money startingValue,
            Money endingValue)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartingValue = startingValue;
            EndingValue = endingValue;
        }

        /// <summary>
        /// Number of days in this period
        /// </summary>
        public int DaysInPeriod => EndDate.DayNumber - StartDate.DayNumber + 1;

        /// <summary>
        /// Calculates the period return accounting for cash flows
        /// </summary>
        public decimal CalculatePeriodReturn()
        {
            var totalCashFlow = CashFlows.Sum(cf => cf.Amount.Amount);
            var adjustedStartingValue = StartingValue.Amount + totalCashFlow;

            if (adjustedStartingValue == 0)
            {
                return 0m;
            }

            return (EndingValue.Amount - adjustedStartingValue) / adjustedStartingValue;
        }
    }
}