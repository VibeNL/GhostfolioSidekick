namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a drawdown event in the portfolio
    /// </summary>
    public record class Drawdown
    {
        /// <summary>
        /// Start date of the drawdown
        /// </summary>
        public DateOnly StartDate { get; init; }

        /// <summary>
        /// End date of the drawdown (when portfolio recovered to previous peak)
        /// </summary>
        public DateOnly? EndDate { get; init; }

        /// <summary>
        /// Date when the maximum drawdown occurred within this period
        /// </summary>
        public DateOnly BottomDate { get; init; }

        /// <summary>
        /// Peak value before the drawdown
        /// </summary>
        public Money PeakValue { get; init; } = new Money();

        /// <summary>
        /// Lowest value during the drawdown
        /// </summary>
        public Money BottomValue { get; init; } = new Money();

        /// <summary>
        /// Value when portfolio recovered (if recovered)
        /// </summary>
        public Money? RecoveryValue { get; init; }

        /// <summary>
        /// Maximum drawdown percentage
        /// </summary>
        public decimal DrawdownPercentage { get; init; }

        /// <summary>
        /// Duration of the drawdown in days
        /// </summary>
        public int? DurationDays { get; init; }

        /// <summary>
        /// Duration to recover to the previous peak (if recovered)
        /// </summary>
        public int? RecoveryDays { get; init; }

        /// <summary>
        /// Whether this drawdown has been recovered
        /// </summary>
        public bool IsRecovered => EndDate.HasValue;

        /// <summary>
        /// Whether this is an ongoing drawdown
        /// </summary>
        public bool IsOngoing => !EndDate.HasValue;

        public Drawdown(
            DateOnly startDate,
            DateOnly bottomDate,
            Money peakValue,
            Money bottomValue)
        {
            StartDate = startDate;
            BottomDate = bottomDate;
            PeakValue = peakValue;
            BottomValue = bottomValue;
            
            if (peakValue.Amount > 0)
            {
                DrawdownPercentage = (peakValue.Amount - bottomValue.Amount) / peakValue.Amount;
            }
        }

        /// <summary>
        /// Marks the drawdown as recovered
        /// </summary>
        public Drawdown WithRecovery(DateOnly recoveryDate, Money recoveryValue)
        {
            return this with
            {
                EndDate = recoveryDate,
                RecoveryValue = recoveryValue,
                DurationDays = BottomDate.DayNumber - StartDate.DayNumber,
                RecoveryDays = recoveryDate.DayNumber - StartDate.DayNumber
            };
        }

        /// <summary>
        /// Absolute amount of the drawdown
        /// </summary>
        public Money DrawdownAmount => new Money(PeakValue.Currency, PeakValue.Amount - BottomValue.Amount);
    }
}