using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using System.Drawing;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record ActivityWithQuantityAndUnitPrice : Activity
	{
		protected ActivityWithQuantityAndUnitPrice() : base()
		{
			// EF Core
		}

		protected ActivityWithQuantityAndUnitPrice(
			Account account,
			DateTime dateTime,
			decimal quantity,
			Money? unitPrice,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			Quantity = quantity;
			UnitPrice = unitPrice;
		}

		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }
	}
}
