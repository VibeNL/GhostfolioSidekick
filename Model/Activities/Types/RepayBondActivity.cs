using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : Activity
	{
		internal RepayBondActivity()
		{
			// EF Core
			TotalRepayAmount = null!;
		}

		public RepayBondActivity(
		Account account,
		DateTime dateTime,
		Money totalRepayAmount,
		string? transactionId,
		int? sortingPriority,
		string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			TotalRepayAmount = totalRepayAmount;
		}

		public Money TotalRepayAmount { get; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
