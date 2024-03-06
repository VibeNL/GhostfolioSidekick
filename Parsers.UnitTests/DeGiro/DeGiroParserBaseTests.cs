using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.DeGiro;

namespace GhostfolioSidekick.Parsers.UnitTests.DeGiro
{
	public class DeGiroParserBaseTests : DeGiroParserBase<TestDeGiroRecordRecord>
	{
		public DeGiroParserBaseTests() : base(new DummyCurrencyMapper())
		{
		}

		[Theory]
		[InlineData(PartialActivityType.Undefined)]
		[InlineData(null)]
		public void Parse_WhenCalled_ReturnsActivity(PartialActivityType? activityType)
		{
			// Arrange
			var record = new TestDeGiroRecordRecord(activityType)
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
			var record = new TestDeGiroRecordRecord(PartialActivityType.StakingReward)
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
		private PartialActivityType? activityType;

		public TestDeGiroRecordRecord(PartialActivityType? activityType)
		{
			this.activityType = activityType;
		}

		public override PartialActivityType? GetActivityType()
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

		public override Currency GetCurrency(ICurrencyMapper currencyMapper)
		{
			throw new NotImplementedException();
		}

		public override void SetGenerateTransactionIdIfEmpty(DateTime recordDate)
		{
		}
	}
}
