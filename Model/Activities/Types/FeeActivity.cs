using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class FeeActivity : ActivityWithAmount
	{
		public FeeActivity()
		{
			// EF Core
			Amount = null!;
		}

		public FeeActivity(
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
