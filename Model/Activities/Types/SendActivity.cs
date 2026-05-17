using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record SendActivity : ActivityWithQuantityAndUnitPrice, IActivityWithCosts
	{
		public SendActivity()
		{
			// EF Core
		}

		public SendActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, amount, new Money(), new Money(), transactionId, sortingPriority, description)
		{
		}

		public List<Money> Fees { get; set; } = [];

		public IReadOnlyList<Money> Costs => Fees;
	}
}
