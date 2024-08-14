using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record CashDepositWithdrawalActivity : Activity
	{
		internal CashDepositWithdrawalActivity() : base()
		{
			// EF Core
			Amount = null!;
		}

		public CashDepositWithdrawalActivity(
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
