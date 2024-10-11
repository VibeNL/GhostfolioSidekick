﻿using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record ActivityWithQuantityAndUnitPrice : Activity
	{
		protected ActivityWithQuantityAndUnitPrice() : base()
		{
			// EF Core
			PartialSymbolIdentifiers = new List<PartialSymbolIdentifier>();
		}

		protected ActivityWithQuantityAndUnitPrice(
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal quantity,
			Money? unitPrice,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = partialSymbolIdentifiers;
			Quantity = quantity;
			UnitPrice = unitPrice;
		}

		public virtual ICollection<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }

		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }
	}
}
