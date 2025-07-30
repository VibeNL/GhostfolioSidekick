namespace GhostfolioSidekick.Model.Performance
{
	public class CalculatedSnapshot
	{
		public long Id { get; set; } // EF Core key
		public DateOnly Date { get; set; }
		public decimal Quantity { get; set; }
		public Money AverageCostPrice { get; set; } = Money.Zero(Currency.USD);
		public Money CurrentUnitPrice { get; set; } = Money.Zero(Currency.USD);
		public Money TotalInvested { get; set; } = Money.Zero(Currency.USD);
		public Money TotalValue { get; set; } = Money.Zero(Currency.USD);

		// Parameterless constructor for EF Core
		public CalculatedSnapshot()
		{
		}

		// Constructor for creating instances
		public CalculatedSnapshot(long id, DateOnly date, decimal quantity, Money averageCostPrice, Money currentUnitPrice, Money totalInvested, Money totalValue)
		{
			Id = id;
			Date = date;
			Quantity = quantity;
			AverageCostPrice = averageCostPrice;
			CurrentUnitPrice = currentUnitPrice;
			TotalInvested = totalInvested;
			TotalValue = totalValue;
		}

		// Constructor for creating instances (without id, for backward compatibility)
		public CalculatedSnapshot(DateOnly date, decimal quantity, Money averageCostPrice, Money currentUnitPrice, Money totalInvested, Money totalValue)
			: this(0, date, quantity, averageCostPrice, currentUnitPrice, totalInvested, totalValue)
		{
		}

		// Copy constructor
		public CalculatedSnapshot(CalculatedSnapshot original)
		{
			Id = original.Id;
			Date = original.Date;
			Quantity = original.Quantity;
			AverageCostPrice = original.AverageCostPrice;
			CurrentUnitPrice = original.CurrentUnitPrice;
			TotalInvested = original.TotalInvested;
			TotalValue = original.TotalValue;
		}

		public static CalculatedSnapshot Empty(Currency currency) => new(0, DateOnly.MinValue, 0, Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));
	}
}