namespace GhostfolioSidekick.Model.Performance
{
	public class CalculatedSnapshotPrimaryCurrency
	{
		public long Id { get; set; } // EF Core key
		public int AccountId { get; set; } // Foreign key to Account, if needed
		public long HoldingAggregatedId { get; set; } // Foreign key to Holding, if needed
		public DateOnly Date { get; set; }
		public decimal Quantity { get; set; }
		public decimal AverageCostPrice { get; set; } = 0;
		public decimal CurrentUnitPrice { get; set; } = 0;
		public decimal TotalInvested { get; set; } = 0;
		public decimal TotalValue { get; set; } = 0;

		// Parameterless constructor for EF Core
		public CalculatedSnapshotPrimaryCurrency()
		{
		}

		public override string ToString()
		{
			return $"CalculatedSnapshotPrimaryCurrency(Id={Id}, AccountId={AccountId}, Date={Date}, Quantity={Quantity}, AverageCostPrice={AverageCostPrice}, CurrentUnitPrice={CurrentUnitPrice}, TotalInvested={TotalInvested}, TotalValue={TotalValue})";
		}
	}
}