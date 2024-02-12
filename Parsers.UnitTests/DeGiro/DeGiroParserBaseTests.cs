using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserBaseTests : DeGiroParserBase<TestDeGiroRecordRecord>
	{
		public DeGiroParserBaseTests()
		{
		}

		[Theory]
		[InlineData(ActivityType.Undefined)]
		[InlineData(null)]
		public void Parse_WhenCalled_ReturnsActivity(ActivityType? activityType)
		{
			// Arrange
			var record = new TestDeGiroRecordRecord(ActivityType.Undefined)
			{
				BalanceCurrency = "EUR",
				Description = "Comissões de transação DEGIRO e/ou taxas de terceiros",
				Mutation = "0,00"
			};

			// Act
			var result = ParseRow(record, 0).ToList();

			// Assert
			result.Count.Should().Be(1);
		}

		[Fact]
		public void Parse_WhenCalledUndefined_ThrowsException()
		{
			// Arrange
			var record = new TestDeGiroRecordRecord(ActivityType.StakingReward)
			{
				BalanceCurrency = "EUR",
				Description = "Comissões de transação DEGIRO e/ou taxas de terceiros",
				Mutation = "0,00"
			};

			// Act
			var a = () => { return ParseRow(record, 0).ToList(); };

			// Assert
			a.Should().Throw<NotSupportedException>();
		}
	}

	public class TestDeGiroRecordRecord : DeGiroRecordBase
	{
		private ActivityType? activityType;

		public TestDeGiroRecordRecord(ActivityType? activityType)
		{
			this.activityType = activityType;
		}

		public override ActivityType? GetActivityType()
		{
			return activityType;
		}

		public override decimal GetQuantity()
		{
			throw new NotImplementedException();
		}

		public override decimal GetUnitPrice()
		{
			throw new NotImplementedException();
		}

		public override Currency GetCurrency()
		{
			throw new NotImplementedException();
		}

		public override void SetGenerateTransactionIdIfEmpty(DateTime recordDate)
		{
		}
	}
}
