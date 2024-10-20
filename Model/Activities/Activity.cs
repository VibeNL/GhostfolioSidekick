using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record Activity
	{
		protected Activity()
		{
			// EF Core
			Account = null!;
		}

		protected Activity(Account account, DateTime date, string? transactionId, int? sortingPriority, string? description)
		{
			Account = account;
			Date = date;
			TransactionId = transactionId;
			SortingPriority = sortingPriority;
			Description = description;
		}

		public long Id { get; set; }

		public virtual Account Account { get; set; }

		public DateTime Date { get; set; }

		public string? TransactionId { get; set; }

		public int? SortingPriority { get; set; }

		public string? Description { get; set; }
	}
}
