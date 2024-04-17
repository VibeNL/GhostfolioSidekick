using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.CentraalBeheer;

namespace GhostfolioSidekick.Parsers.UnitTests.CentraalBeheer
{
	public class CentraalBeheerParserTests
	{
		readonly CentraalBeheerParser parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public CentraalBeheerParserTests()
		{
			parser = new CentraalBeheerParser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/CentraalBeheer/", "*.pdf", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/deposit.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[PartialActivity.CreateCashDeposit(
					Currency.EUR,
					new DateTime(2023, 9, 9, 0, 0, 0, DateTimeKind.Utc),
					1000,
					new Money(Currency.EUR, 1000),
					"Centraal_Beheer_CashDeposit_2023-09-09")]
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/buy_order.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 09, 12, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Ambitieus")],
						29.3667M,
						33.95M,
						new Money(Currency.EUR, 1000M),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Ambitieus_2023-09-12"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 09, 12, 0, 0, 0, DateTimeKind.Utc),
						3M,
						new Money(Currency.EUR, 0),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Ambitieus_2023-09-12")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_TestFileSingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/CentraalBeheer/sell_order.pdf", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 7, 26, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateStockAndETF("Centraal Beheer Mixfonds Voorzichtig")],
						1.9314M,
						26.28M,
						new Money(Currency.EUR,  50.76M),
						"Centraal_Beheer_Buy_Centraal Beheer Mixfonds Voorzichtig_2023-07-26")
				]);
		}
	}
}