﻿using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.ScalableCaptial;

namespace Parsers.UnitTests.ScalableCapital
{
	public class ScalableCapitalWUMParserTests
	{
		private ScalableCapitalWUMParser parser;
		private Account account;
		private TestHoldingsAndAccountsCollection holdingsAndAccountsCollection;

		public ScalableCapitalWUMParserTests()
		{
			parser = new ScalableCapitalWUMParser();

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(Currency.EUR))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsAndAccountsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/ScalableCapital/", "wum.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task CanParseActivities_SingleBuy_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/BuyOrders/SingleBuy/wum.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc), "IE00077FRP95", 5, 8.685M, "SCALQbWiZnN9DtQ")
				]);
		}

		[Fact]
		public async Task CanParseActivities_SingleSell_CorrectlyParsed()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/ScalableCapital/SellOrders/SingleSell/wum.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(Currency.EUR, new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc), "IE00077FRP95", 5, 8.685M, "SCALQbWiZnN9DtQ")
				]);
		}
	}
}