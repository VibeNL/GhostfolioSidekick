using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

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
		string? transactionId) : base(account, dateTime, amount, null, transactionId, null, null)
		{
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
