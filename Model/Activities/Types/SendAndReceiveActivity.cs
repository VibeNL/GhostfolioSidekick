using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record SendAndReceiveActivity : ActivityWithQuantityAndUnitPrice
	{
		public SendAndReceiveActivity()
		{
			// EF Core
		}

		public SendAndReceiveActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, amount, new Money(), transactionId, sortingPriority, description)
		{
		}

		public virtual ICollection<SendAndReceiveActivityFee> Fees { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
