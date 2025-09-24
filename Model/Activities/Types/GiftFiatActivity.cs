using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftFiatActivity : ActivityWithAmount
	{
		public GiftFiatActivity()
		{
			// EF Core
			Amount = null!;
		}

		public GiftFiatActivity(
			Account account,
			Holding? holding,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, amount, transactionId, sortingPriority, description)
		{
		}
	}
}
