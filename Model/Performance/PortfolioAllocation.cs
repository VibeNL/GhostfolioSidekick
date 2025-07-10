namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents portfolio allocation breakdown by various dimensions
    /// </summary>
    public record class PortfolioAllocation
    {
        /// <summary>
        /// Date for which this allocation is calculated
        /// </summary>
        public DateOnly Date { get; init; }

        /// <summary>
        /// Total portfolio value
        /// </summary>
        public Money TotalValue { get; init; } = new Money();

        /// <summary>
        /// Cash allocation
        /// </summary>
        public Money CashValue { get; init; } = new Money();

        /// <summary>
        /// Allocation by asset class
        /// </summary>
        public IReadOnlyDictionary<Activities.AssetClass, AllocationItem> AssetClassAllocation { get; init; } = 
            new Dictionary<Activities.AssetClass, AllocationItem>();

        /// <summary>
        /// Allocation by asset sub-class
        /// </summary>
        public IReadOnlyDictionary<Activities.AssetSubClass, AllocationItem> AssetSubClassAllocation { get; init; } = 
            new Dictionary<Activities.AssetSubClass, AllocationItem>();

        /// <summary>
        /// Allocation by currency
        /// </summary>
        public IReadOnlyDictionary<Currency, AllocationItem> CurrencyAllocation { get; init; } = 
            new Dictionary<Currency, AllocationItem>();

        /// <summary>
        /// Allocation by sector (if available)
        /// </summary>
        public IReadOnlyDictionary<string, AllocationItem> SectorAllocation { get; init; } = 
            new Dictionary<string, AllocationItem>();

        /// <summary>
        /// Allocation by country (if available)
        /// </summary>
        public IReadOnlyDictionary<string, AllocationItem> CountryAllocation { get; init; } = 
            new Dictionary<string, AllocationItem>();

        /// <summary>
        /// Allocation by account
        /// </summary>
        public IReadOnlyDictionary<int, AllocationItem> AccountAllocation { get; init; } = 
            new Dictionary<int, AllocationItem>();

        /// <summary>
        /// Individual holding allocations
        /// </summary>
        public IReadOnlyList<HoldingAllocation> HoldingAllocations { get; init; } = [];

        public PortfolioAllocation(DateOnly date, Money totalValue)
        {
            Date = date;
            TotalValue = totalValue;
        }

        /// <summary>
        /// Cash percentage of total portfolio
        /// </summary>
        public decimal CashPercentage => TotalValue.Amount == 0 ? 0 : CashValue.Amount / TotalValue.Amount;

        /// <summary>
        /// Gets allocation for the largest asset class
        /// </summary>
        public AllocationItem? LargestAssetClassAllocation => 
            AssetClassAllocation.Values.MaxBy(x => x.Percentage);

        /// <summary>
        /// Gets the most concentrated holding
        /// </summary>
        public HoldingAllocation? MostConcentratedHolding => 
            HoldingAllocations.MaxBy(x => x.Percentage);
    }

    /// <summary>
    /// Represents an allocation item with value and percentage
    /// </summary>
    public record class AllocationItem
    {
        /// <summary>
        /// Value of this allocation
        /// </summary>
        public Money Value { get; init; } = new Money();

        /// <summary>
        /// Percentage of total portfolio
        /// </summary>
        public decimal Percentage { get; init; }

        /// <summary>
        /// Number of holdings in this allocation
        /// </summary>
        public int HoldingCount { get; init; }

        public AllocationItem(Money value, decimal percentage, int holdingCount = 1)
        {
            Value = value;
            Percentage = percentage;
            HoldingCount = holdingCount;
        }
    }

    /// <summary>
    /// Represents allocation for a specific holding
    /// </summary>
    public record class HoldingAllocation
    {
        /// <summary>
        /// The holding
        /// </summary>
        public Holding Holding { get; init; } = null!;

        /// <summary>
        /// Value of this holding
        /// </summary>
        public Money Value { get; init; } = new Money();

        /// <summary>
        /// Percentage of total portfolio
        /// </summary>
        public decimal Percentage { get; init; }

        /// <summary>
        /// Quantity held
        /// </summary>
        public decimal Quantity { get; init; }

        /// <summary>
        /// Current unit price
        /// </summary>
        public Money UnitPrice { get; init; } = new Money();

        /// <summary>
        /// Cost basis of this holding
        /// </summary>
        public Money CostBasis { get; init; } = new Money();

        /// <summary>
        /// Unrealized gain/loss
        /// </summary>
        public Money UnrealizedGainLoss { get; init; } = new Money();

        /// <summary>
        /// Unrealized gain/loss percentage
        /// </summary>
        public decimal UnrealizedGainLossPercentage { get; init; }

        public HoldingAllocation(Holding holding, Money value, decimal percentage, decimal quantity)
        {
            Holding = holding;
            Value = value;
            Percentage = percentage;
            Quantity = quantity;
        }

        /// <summary>
        /// Primary symbol for this holding
        /// </summary>
        public string? Symbol => Holding.SymbolProfiles.FirstOrDefault()?.Symbol;

        /// <summary>
        /// Name of this holding
        /// </summary>
        public string? Name => Holding.SymbolProfiles.FirstOrDefault()?.Name;
    }
}