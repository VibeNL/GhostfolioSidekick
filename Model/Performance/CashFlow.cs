namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a cash flow event (deposit, withdrawal, dividend, etc.)
    /// </summary>
    public record class CashFlow
    {
        /// <summary>
        /// Date of the cash flow
        /// </summary>
        public DateOnly Date { get; init; }

        /// <summary>
        /// Amount of the cash flow (positive for inflows, negative for outflows)
        /// </summary>
        public Money Amount { get; init; } = new Money();

        /// <summary>
        /// Type of cash flow
        /// </summary>
        public CashFlowType Type { get; init; }

        /// <summary>
        /// Account associated with this cash flow
        /// </summary>
        public Accounts.Account? Account { get; init; }

        /// <summary>
        /// Holding associated with this cash flow (if applicable)
        /// </summary>
        public Holding? Holding { get; init; }

        /// <summary>
        /// Description of the cash flow
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Reference to the original activity that generated this cash flow
        /// </summary>
        public long? ActivityId { get; init; }

        public CashFlow(DateOnly date, Money amount, CashFlowType type)
        {
            Date = date;
            Amount = amount;
            Type = type;
        }

        /// <summary>
        /// Whether this is a cash inflow (positive amount)
        /// </summary>
        public bool IsInflow => Amount.Amount > 0;

        /// <summary>
        /// Whether this is a cash outflow (negative amount)
        /// </summary>
        public bool IsOutflow => Amount.Amount < 0;

        /// <summary>
        /// Absolute value of the cash flow
        /// </summary>
        public Money AbsoluteAmount => new Money(Amount.Currency, Math.Abs(Amount.Amount));
    }

    /// <summary>
    /// Types of cash flows for performance calculation
    /// </summary>
    public enum CashFlowType
    {
        /// <summary>
        /// External deposit into the portfolio
        /// </summary>
        Deposit,

        /// <summary>
        /// External withdrawal from the portfolio
        /// </summary>
        Withdrawal,

        /// <summary>
        /// Dividend received
        /// </summary>
        Dividend,

        /// <summary>
        /// Interest received
        /// </summary>
        Interest,

        /// <summary>
        /// Fee paid
        /// </summary>
        Fee,

        /// <summary>
        /// Tax paid
        /// </summary>
        Tax,

        /// <summary>
        /// Purchase of securities
        /// </summary>
        Purchase,

        /// <summary>
        /// Sale of securities
        /// </summary>
        Sale,

        /// <summary>
        /// Transfer between accounts
        /// </summary>
        Transfer,

        /// <summary>
        /// Other type of cash flow
        /// </summary>
        Other
    }
}