using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class LiabilityActivity : Activity
	{
		public LiabilityActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, transactionId, null, null)
		{
			Price = amount;
		}

		public Money Price { get; set; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
