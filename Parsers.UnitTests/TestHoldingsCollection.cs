using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	internal class TestHoldingsCollection(string AccountName) : IHoldingsCollection
	{
		public List<PartialActivity> PartialActivities { get; set; } = [];

		public void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			accountName.Should().Be(AccountName);
			PartialActivities.AddRange(partialActivities);
		}

		public Task<IEnumerable<Holding>> GenerateActivities()
		{
			throw new NotImplementedException();
		}
		}
}
