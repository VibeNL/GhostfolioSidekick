using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record KnownBalanceActivity : Activity
	{
		internal KnownBalanceActivity()
		{
			// EF Core
			Amount = null!;
		}

		public KnownBalanceActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public Money Amount { get; set; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
