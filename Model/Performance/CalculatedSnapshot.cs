namespace GhostfolioSidekick.Model.Performance
{
	public class CalculatedSnapshot
	{
		public Guid Id { get; set; } // EF Core key
		public int AccountId { get; set; } // Foreign key to Account, if needed
		public long HoldingId { get; set; } // Foreign key to Holding, if needed
		public DateOnly Date { get; set; }
		public decimal Quantity { get; set; }
		public Currency Currency { get; set; }
		public decimal AverageCostPrice { get; set; }
		public decimal CurrentUnitPrice { get; set; }
		public decimal TotalInvested { get; set; }
		public decimal TotalValue { get; set; }

		// Parameterless constructor for EF Core
		public CalculatedSnapshot()
		{
			Id = Guid.NewGuid();
			Currency = default!;
			AverageCostPrice = default;
			CurrentUnitPrice = default;
			TotalInvested = default;
			TotalValue = default;
		}

		public CalculatedSnapshot(
			Guid id,
			int accountId,
			DateOnly date,
			decimal quantity,
			Currency currency,
			decimal averageCostPrice,
			decimal currentUnitPrice,
			decimal totalInvested,
			decimal totalValue)
		{
			Id = id;
			AccountId = accountId;
			Date = date;
			Quantity = quantity;
			Currency = currency;
			AverageCostPrice = averageCostPrice;
			CurrentUnitPrice = currentUnitPrice;
			TotalInvested = totalInvested;
			TotalValue = totalValue;
		}

		public CalculatedSnapshot(CalculatedSnapshot original)
		{
			Id = Guid.NewGuid();
			Date = original.Date;
			Quantity = original.Quantity;
			Currency = original.Currency;
			AverageCostPrice = original.AverageCostPrice;
			CurrentUnitPrice = original.CurrentUnitPrice;
			TotalInvested = original.TotalInvested;
			TotalValue = original.TotalValue;
			AccountId = original.AccountId;
		}

		public static CalculatedSnapshot Empty(Currency currency, int accountId) => 
			new(Guid.NewGuid(), accountId, DateOnly.MinValue, 0, currency, 0, 0, 0, 0);

		public override string ToString()
		{
			return $"CalculatedSnapshot(Id={Id}, AccountId={AccountId}, Date={Date}, Quantity={Quantity}, Currency={Currency}, AverageCostPrice={AverageCostPrice}, CurrentUnitPrice={CurrentUnitPrice}, TotalInvested={TotalInvested}, TotalValue={TotalValue})";
		}
	}
}