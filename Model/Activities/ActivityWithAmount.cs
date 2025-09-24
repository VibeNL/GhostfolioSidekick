using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record ActivityWithAmount : Activity
	{
		protected ActivityWithAmount() : base()
		{
			// EF Core
			Amount = new Money();
		}

		protected ActivityWithAmount(
			Account account,
			Holding? holding,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public Money Amount { get; set; }
	}
}
