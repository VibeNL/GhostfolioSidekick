using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record Activity
	{
		protected Activity()
		{
			// EF Core
			Account = null!;
			TransactionId = null!;
		}

		protected Activity(Account account, Holding? holding, DateTime date, string transactionId, int? sortingPriority, string? description)
		{
			Account = account;
			Holding = holding;
			Date = date;
			TransactionId = transactionId;
			SortingPriority = sortingPriority;
			Description = description;
		}

		public long Id { get; set; }

		public virtual Account Account { get; set; }

		public Holding? Holding { get; set; }

		public DateTime Date { get; set; }

		public string TransactionId { get; set; }

		public int? SortingPriority { get; set; }

		public string? Description { get; set; }
	}
}
