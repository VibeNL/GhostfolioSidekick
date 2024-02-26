using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.SpecificActivity
{
	public record class BuySellActivity : IActivity
	{
		public BuySellActivity(
		Account account,
		DateTime dateTime,
		decimal quantity,
		Money? unitPrice,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Quantity = quantity;
			UnitPrice = unitPrice;
			TransactionId = transactionId;
		}

		public Account Account { get; }
		
		public DateTime Date { get; }

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public string? TransactionId { get; set; }

		public int? SortingPriority { get; set; }

		public string? Description { get; set; }

		public string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
