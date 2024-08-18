using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class ValuableActivity : Activity
	{
		internal ValuableActivity()
		{
			// EF Core
			Price = null!;
		}

		public ValuableActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
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
