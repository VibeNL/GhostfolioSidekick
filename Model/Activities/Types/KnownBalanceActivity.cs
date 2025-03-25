using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record KnownBalanceActivity : Activity
	{
		public KnownBalanceActivity()
		{
			// EF Core
			Amount = null!;
		}

		public KnownBalanceActivity(
			Account account,
			Holding? holding,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public Money Amount { get; set; }
	}
}
