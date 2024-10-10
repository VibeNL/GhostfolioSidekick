using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class InterestActivity : Activity
	{
		internal InterestActivity()
		{
			// EF Core
			Amount = null!;
		}

		public InterestActivity(
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
