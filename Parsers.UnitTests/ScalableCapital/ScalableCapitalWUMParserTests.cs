﻿using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using GhostfolioSidekick.Parsers.UnitTests;

namespace GhostfolioSidekick.Parsers.UnitTests.ScalableCapital
{
	public class ScalableCapitalWUMParserTests
	{
		private ScalableCapitalWUMParser parser;
		private Account account;
		private TestHoldingsCollection holdingsAndAccountsCollection;

		public ScalableCapitalWUMParserTests()
		{
			parser = new ScalableCapitalWUMParser();

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
					PartialActivity.CreateBuy(Currency.EUR, new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateStockAndETF("IE00077FRP95")], 5, 8.685M, "SCALQbWiZnN9DtQ")
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
					PartialActivity.CreateSell(Currency.EUR, new DateTime(2023, 8, 3, 14, 43, 17, 650, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateStockAndETF("IE00077FRP95")], 5, 8.685M, "SCALQbWiZnN9DtQ")
				]);
		}
	}
}
