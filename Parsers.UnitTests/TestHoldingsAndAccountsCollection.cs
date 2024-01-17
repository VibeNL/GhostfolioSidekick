using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;

namespace Parsers.UnitTests
{
	internal class TestHoldingsAndAccountsCollection : IHoldingsAndAccountsCollection
	{
		private static readonly List<Holding> holdings = [];
		private static readonly List<Account> accounts = [];

		public IReadOnlyList<Account> Accounts { get; set; }
		public IReadOnlyList<Holding> Holdings { get; set; }

		public List<PartialActivity> PartialActivities { get; set; } = [];

		public TestHoldingsAndAccountsCollection(Account account)
		{
			accounts.Add(account);
		}

		public void AddPartialActivity(Account account, IEnumerable<PartialActivity> partialActivity)
		{
			account.Should().Be(Accounts.Single());

			PartialActivities.AddRange(partialActivity);
		}

		public Account GetAccount(string accountName)
		{
			return accounts.Single(x => x.Name == accountName);
		}
	}
}
