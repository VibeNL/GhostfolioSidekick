using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuySellActivity : ActivityWithQuantityAndUnitPrice
	{
		internal BuySellActivity()
		{
			// EF Core
		}

		public BuySellActivity(
		Account account,
		DateTime dateTime,
		decimal quantity,
		Money? unitPrice,
		string? transactionId,
		int? sortingPriority,
		string? description) : base(account, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		public IEnumerable<Money> Taxes { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
