using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class FeeActivity : Activity
	{
		public FeeActivity()
		{
			// EF Core
			Amount = null!;
		}

		public FeeActivity(
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
