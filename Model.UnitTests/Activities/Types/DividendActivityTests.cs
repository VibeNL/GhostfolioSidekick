using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using AutoFixture;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class DividendActivityTests
	{
		private readonly DividendActivity activity;

		public DividendActivityTests()
		{
			var account = new Fixture().Create<Account>();
			var symbolProfile = new Fixture().Create<SymbolProfile>();
			var dateTime = DateTime.Now;
			var amount = new Money(Currency.EUR, 1);
			var transactionId = "transactionId";

			activity = new DividendActivity(symbolProfile, account, [], dateTime, amount, transactionId, null, null);
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
