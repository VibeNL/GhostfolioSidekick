using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	internal class TestHoldingsCollection : IHoldingsCollection
	{
		private readonly List<Holding> holdings = [];
		private readonly Account account;

		public IReadOnlyList<Holding> Holdings => holdings;

		public List<PartialActivity> PartialActivities { get; set; } = [];

		public TestHoldingsCollection(Account account)
		{
			this.account = account;
		}

		public Task AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			accountName.Should().Be(account.Name);
			PartialActivities.AddRange(partialActivities);
			return Task.CompletedTask;
		}
	}
}
