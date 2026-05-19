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
			Money transactionAmount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			Quantity = quantity;
			UnitPrice = unitPrice;
			TransactionAmount = transactionAmount;
			AdjustedQuantity = 0;
			AdjustedUnitPrice = new Money();
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public decimal Quantity { get; set; }

		public Money UnitPrice { get; set; }

		public Money TransactionAmount { get; set; } = new Money();

		public decimal AdjustedQuantity { get; set; }

		public Money AdjustedUnitPrice { get; set; }

		public virtual List<CalculatedPriceTrace> AdjustedUnitPriceSource { get; set; } = [];
	}
}
