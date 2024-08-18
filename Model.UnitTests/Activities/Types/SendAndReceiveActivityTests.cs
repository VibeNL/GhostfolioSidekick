using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using AutoFixture;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class SendAndReceiveActivityTests
	{
		private readonly SendAndReceiveActivity activity;

		public SendAndReceiveActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var dateTime = DateTime.Now;
			var quantity = 10m;
			var transactionId = "transactionId";

			activity = new SendAndReceiveActivity(account, dateTime, quantity, transactionId, null, null);
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
