using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.ScalableCaptial;

namespace Parsers.UnitTests.ScalableCapital
{
	public class ScalableCapitalRKKParserTests
	{
		private ScalableCapitalRKKParser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;

		public ScalableCapitalRKKParserTests()
		{
			parser = new ScalableCapitalRKKParser();

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
			foreach (var file in Directory.GetFiles("./TestFiles/ScalableCapital/", "rkk.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task CanParseActivities_SingleKnownSaldo_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/CashTransactions/single_known_saldo.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateKnownBalance(Currency.EUR, new DateTime(2023, 04, 11, 00, 00, 00, DateTimeKind.Utc), 21.68M)
				]);
		}

		[Fact]
		public async Task CanParseActivities_SingleKnownDividend_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/CashTransactions/single_dividend.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(Currency.EUR, new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Utc), ["US92343V1044"], 14 * 0.5057142857142857142857142857M, "WWEK 16100100")
				]);
		}

		[Fact]
		public async Task CanParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/BuyOrders/SingleBuy/rkk.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 8, 2, 0, 0, 0, 0, DateTimeKind.Utc), 0.99M, "SCALQbWiZnN9DtQ")
				]);
		}

		[Fact]
		public async Task CanParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/SellOrders/SingleSell/rkk.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateFee(Currency.EUR, new DateTime(2023, 8, 2, 0, 0, 0, 0, DateTimeKind.Utc), 0.99M, "SCALQbWiZnN9DtQ")
				]);
		}
	}
}
