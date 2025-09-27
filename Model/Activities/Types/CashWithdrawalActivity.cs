using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record CashWithdrawalActivity : ActivityWithAmount
	{
		public CashWithdrawalActivity()
		{
			// EF Core
			Amount = null!;
		}

		public CashWithdrawalActivity(
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
