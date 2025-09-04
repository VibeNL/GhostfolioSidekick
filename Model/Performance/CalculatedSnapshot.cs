namespace GhostfolioSidekick.Model.Performance
{
	public class CalculatedSnapshot
	{
		public long Id { get; set; } // EF Core key
		public int AccountId { get; set; } // Foreign key to Account, if needed
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Major Code Smell", "S107:Methods should not have too many parameters", 
			Justification = "DDD")]
		public CalculatedSnapshot(long id, int accountId, DateOnly date, decimal quantity, Money averageCostPrice, Money currentUnitPrice, Money totalInvested, Money totalValue)
		{
			Id = id;
			AccountId = accountId;
			Date = date;
			Quantity = quantity;
			AverageCostPrice = averageCostPrice;
			CurrentUnitPrice = currentUnitPrice;
			TotalInvested = totalInvested;
			TotalValue = totalValue;
		}

		public CalculatedSnapshot(CalculatedSnapshot original)
		{
			Id = original.Id;
			Date = original.Date;
			Quantity = original.Quantity;
			AverageCostPrice = original.AverageCostPrice;
			CurrentUnitPrice = original.CurrentUnitPrice;
			TotalInvested = original.TotalInvested;
			TotalValue = original.TotalValue;
			AccountId = original.AccountId;
		}

		public static CalculatedSnapshot Empty(Currency currency, int accountId) => new(0, accountId, DateOnly.MinValue, 0, Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));

		public override string ToString()
		{
			return $"CalculatedSnapshot(Id={Id}, AccountId={AccountId}, Date={Date}, Quantity={Quantity}, AverageCostPrice={AverageCostPrice}, CurrentUnitPrice={CurrentUnitPrice}, TotalInvested={TotalInvested}, TotalValue={TotalValue})";
		}
	}
}