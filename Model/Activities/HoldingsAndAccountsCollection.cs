using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public class HoldingsAndAccountsCollection
	{
		private static readonly List<Holding> holdings = [];
		private static readonly List<Account> accounts = [];

		public IReadOnlyList<Holding> Holdings { get; set; } = holdings;

		public IReadOnlyList<Account> Account { get; set; } = accounts;

		public void AddPartialActivity(IEnumerable<PartialActivity> partialActivity)
		{
			throw new NotImplementedException();
		}

		public Account GetAccount(string accountName)
		{
			throw new NotImplementedException();
		}
	}
}
