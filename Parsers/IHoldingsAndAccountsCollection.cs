using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public interface IHoldingsAndAccountsCollection
	{
		IReadOnlyList<Account> Accounts { get; }

		IReadOnlyList<Holding> Holdings { get; }

		void AddPartialActivity(Account account, IEnumerable<PartialActivity> partialActivity);

		Account GetAccount(string accountName);
	}
}