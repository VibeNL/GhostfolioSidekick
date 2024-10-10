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

		public long Id { get; protected set; }

		public Account Account { get; protected set; }

		public DateTime Date { get; protected set; }

		public string? TransactionId { get; protected set; }

		public int? SortingPriority { get; protected set; }

		public string? Description { get; protected set; }
	}
}
