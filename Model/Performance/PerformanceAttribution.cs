namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents performance attribution showing how different holdings contributed to overall performance
    /// </summary>
    public record class PerformanceAttribution
    {
        /// <summary>
        /// The holding this attribution relates to
        /// </summary>
        public Holding Holding { get; init; } = null!;

        /// <summary>
        /// The time period for this attribution
        /// </summary>
        public PerformancePeriod Period { get; init; } = null!;

        /// <summary>
        /// Weight of this holding in the portfolio at the beginning of the period
        /// </summary>
        public decimal StartingWeight { get; init; }

        /// <summary>
        /// Weight of this holding in the portfolio at the end of the period
        /// </summary>
        public decimal EndingWeight { get; init; }

        /// <summary>
        /// Average weight during the period
        /// </summary>
        public decimal AverageWeight { get; init; }

        /// <summary>
        /// Return of this holding during the period
        /// </summary>
        public decimal HoldingReturn { get; init; }

        /// <summary>
        /// Contribution to total portfolio return
        /// </summary>
        public decimal ContributionToReturn { get; init; }

        /// <summary>
        /// Absolute contribution to portfolio performance (in money terms)
        /// </summary>
        public Money AbsoluteContribution { get; init; } = new Money();

        /// <summary>
        /// Starting value of this holding
        /// </summary>
        public Money StartingValue { get; init; } = new Money();

        /// <summary>
        /// Ending value of this holding
        /// </summary>
        public Money EndingValue { get; init; } = new Money();

        /// <summary>
        /// Net cash flow for this holding during the period
        /// </summary>
        public Money NetCashFlow { get; init; } = new Money();

        /// <summary>
        /// Dividends received from this holding
        /// </summary>
        public Money Dividends { get; init; } = new Money();

        /// <summary>
        /// Fees associated with this holding
        /// </summary>
        public Money Fees { get; init; } = new Money();

        /// <summary>
        /// Asset class of the holding
        /// </summary>
        public Activities.AssetClass? AssetClass { get; init; }

        /// <summary>
        /// Asset sub-class of the holding
        /// </summary>
        public Activities.AssetSubClass? AssetSubClass { get; init; }

        /// <summary>
        /// Sector of the holding (if applicable)
        /// </summary>
        public string? Sector { get; init; }

        /// <summary>
        /// Country of the holding (if applicable)
        /// </summary>
        public string? Country { get; init; }

        public PerformanceAttribution(
            Holding holding,
            PerformancePeriod period,
            decimal startingWeight,
            decimal endingWeight,
            decimal holdingReturn,
            Money startingValue,
            Money endingValue)
        {
            Holding = holding;
            Period = period;
            StartingWeight = startingWeight;
            EndingWeight = endingWeight;
            AverageWeight = (startingWeight + endingWeight) / 2;
            HoldingReturn = holdingReturn;
            StartingValue = startingValue;
            EndingValue = endingValue;
            ContributionToReturn = AverageWeight * holdingReturn;
        }

        /// <summary>
        /// Whether this holding was a positive contributor to performance
        /// </summary>
        public bool IsPositiveContributor => ContributionToReturn > 0;

        /// <summary>
        /// The primary symbol for this holding (if available)
        /// </summary>
        public string? PrimarySymbol => Holding.SymbolProfiles.FirstOrDefault()?.Symbol;

        /// <summary>
        /// The name of the holding (if available)
        /// </summary>
        public string? HoldingName => Holding.SymbolProfiles.FirstOrDefault()?.Name;
    }
}