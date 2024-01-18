using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;

namespace GhostfolioSidekick.FileImporter
{
	internal class HoldingsAndAccountsCollection : IHoldingsAndAccountsCollection
	{
		public HoldingsAndAccountsCollection()
		{
		}

		public IReadOnlyList<Account> Accounts => throw new NotImplementedException();

		public IReadOnlyList<Holding> Holdings => throw new NotImplementedException();

		public void AddPartialActivity(Account account, IEnumerable<PartialActivity> partialActivity)
		{
			throw new NotImplementedException();
		}

		public Account GetAccount(string accountName)
		{
			throw new NotImplementedException();
		}
	}
}