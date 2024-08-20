using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record SendAndReceiveActivity : ActivityWithQuantityAndUnitPrice
	{
		internal SendAndReceiveActivity()
		{
		}

		public SendAndReceiveActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId,
		int? sortingPriority,
		string? description) : base(account, dateTime, amount, null, transactionId, sortingPriority, description)
		{
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
