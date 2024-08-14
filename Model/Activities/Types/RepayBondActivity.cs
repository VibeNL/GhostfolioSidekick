using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : Activity
	{
		public RepayBondActivity(
		Account account,
		DateTime dateTime,
		Money totalRepayAmount,
		string? transactionId) : base(account, dateTime, transactionId, null, null)
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
