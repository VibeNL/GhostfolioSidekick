using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : Activity
	{
		internal DividendActivity()
		{
			// EF Core
			Amount = null!;
		}

		public DividendActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public ICollection<Money> Fees { get; set; } = [];

		public Money Amount { get; set; }

		public ICollection<Money> Taxes { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
