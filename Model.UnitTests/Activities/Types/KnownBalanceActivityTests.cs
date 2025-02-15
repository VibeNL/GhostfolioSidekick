using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using AutoFixture;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class KnownBalanceActivityTests
	{
		private readonly KnownBalanceActivity activity;

		public KnownBalanceActivityTests()
		{
			var account = CustomFixture.New().Create<Account>();
			var dateTime = DateTime.Now;
			var amount = new Money(Currency.EUR, 1);
			var transactionId = "transactionId";

			activity = new KnownBalanceActivity(account, new Holding(), dateTime, amount, transactionId, null, null);
		}

		[Fact]
		public void ToString_ShouldReturnExpectedFormat()
		{
			// Arrange
			var expectedFormat = $"{activity.Account}_{activity.Date}";

			// Act
			var result = activity.ToString();

			// Assert
			result.Should().Be(expectedFormat);
		}
	}
}
