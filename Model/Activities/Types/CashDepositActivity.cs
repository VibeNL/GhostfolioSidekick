using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record CashDepositActivity : ActivityWithAmount
	{
		public CashDepositActivity()
		{
			// EF Core
			Amount = null!;
		}

		public CashDepositActivity(
			Account account,
			Holding? holding,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, amount, transactionId, sortingPriority, description)
		{
		}
	}
}
