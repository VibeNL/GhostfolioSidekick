using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class InterestActivity : Activity
	{
		public InterestActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, transactionId, null, null)
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
