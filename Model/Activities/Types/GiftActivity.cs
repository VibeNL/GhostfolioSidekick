using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftActivity : ActivityWithQuantityAndUnitPrice
	{
		internal GiftActivity()
		{			
		}

		public GiftActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId,
		int? sortingPriority,
		string? description) : base(account, dateTime, amount, null, transactionId, sortingPriority, description)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
