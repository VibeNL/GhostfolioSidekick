namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a snapshot of portfolio/holding value at a specific point in time
    /// Used for calculating time-series performance metrics
    /// </summary>
    public record class PerformanceSnapshot
    {
        /// <summary>
        /// The date of this snapshot
        /// </summary>
        public DateOnly Date { get; init; }

        /// <summary>
        /// Total value at this point in time
        /// </summary>
        public Money TotalValue { get; init; } = new Money();

        /// <summary>
        /// Cash position at this point in time
        /// </summary>
        public Money CashValue { get; init; } = new Money();

        /// <summary>
        /// Market value of holdings at this point in time
        /// </summary>
        public Money MarketValue { get; init; } = new Money();

        /// <summary>
        /// Net deposits/withdrawals on this date
        /// </summary>
        public Money NetCashFlow { get; init; } = new Money();

        /// <summary>
        /// Dividends received on this date
        /// </summary>
        public Money Dividends { get; init; } = new Money();

        /// <summary>
        /// Fees paid on this date
        /// </summary>
        public Money Fees { get; init; } = new Money();

        /// <summary>
        /// Daily return percentage (compared to previous day)
        /// </summary>
        public decimal? DailyReturn { get; init; }

        /// <summary>
        /// The account this snapshot relates to (optional - null for portfolio-wide snapshots)
        /// </summary>
        public Accounts.Account? Account { get; init; }

        /// <summary>
        /// The holding this snapshot relates to (optional - null for account/portfolio-wide snapshots)
        /// </summary>
        public Holding? Holding { get; init; }

        public PerformanceSnapshot(DateOnly date, Money totalValue)
        {
            Date = date;
            TotalValue = totalValue;
        }

        /// <summary>
        /// Calculates the daily return compared to a previous snapshot
        /// </summary>
        public decimal CalculateDailyReturn(PerformanceSnapshot previousSnapshot)
        {
            if (previousSnapshot.TotalValue.Amount == 0)
            {
                return 0m;
            }

            // Account for cash flows in return calculation
            var adjustedPreviousValue = previousSnapshot.TotalValue.Amount + NetCashFlow.Amount;
            
            if (adjustedPreviousValue == 0)
            {
                return 0m;
            }

            return (TotalValue.Amount - adjustedPreviousValue) / adjustedPreviousValue;
        }
    }
}