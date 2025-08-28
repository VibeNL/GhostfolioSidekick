using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record ActivityWithQuantityAndUnitPrice : Activity, IActivityWithPartialIdentifier
	{
		protected ActivityWithQuantityAndUnitPrice() : base()
		{
			// EF Core
			UnitPrice = new Money();
			AdjustedUnitPrice = new Money();
		}

		protected ActivityWithQuantityAndUnitPrice(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal quantity,
			Money unitPrice,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			Quantity = quantity;
			UnitPrice = unitPrice;
			AdjustedQuantity = 0;
			AdjustedUnitPrice = new Money();
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public decimal Quantity { get; set; }

		public Money UnitPrice { get; set; }

		public decimal AdjustedQuantity { get; set; }

		public Money AdjustedUnitPrice { get; set; }

		public Money TotalTransactionAmount { get; set; } = new Money();

		public virtual List<CalculatedPriceTrace> AdjustedUnitPriceSource { get; set; } = [];
	}
}
