using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record ActivityWithQuantityAndUnitPrice : Activity, IActivityWithPartialIdentifier
	{
		protected ActivityWithQuantityAndUnitPrice() : base()
		{
			// EF Core
			SymbolProfile = default!;
		}

		protected ActivityWithQuantityAndUnitPrice(
			SymbolProfile? symbolProfile,
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal quantity,
			Money? unitPrice,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			SymbolProfile = symbolProfile;
			Quantity = quantity;
			UnitPrice = unitPrice;
		}

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public SymbolProfile? SymbolProfile { get; }
		
		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }
	}
}
