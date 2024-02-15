using FluentAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Generic;
using GhostfolioSidekick.Parsers.ScalableCaptial;
using Moq;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public class RecordBaseImporterTests
	{
		private const string Filename = "./TestFiles/Generic/BuyOrders/single_buy.csv";
		private GenericParser importer;

		public RecordBaseImporterTests()
		{
			importer = new GenericParser(DummyCurrencyMapper.Instance);
		}

		[Fact]
		public async Task CanParseActivities_ShouldReturnTrue_WhenHeaderIsValid()
		{
			// Arrange

			// Act
			var result = await importer.CanParseActivities(Filename);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task CanParseActivities_ShouldReturnFalse_WhenWrongParser()
		{
			// Arrange

			// Act
			var result = await new ScalableCapitalWUMParser(DummyCurrencyMapper.Instance).CanParseActivities(Filename);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task ParseActivities_ShouldParseRecordsAndAddToCollection()
		{
			// Arrange
			var holdingsAndAccountsCollection = new Mock<IHoldingsCollection>();
			var accountName = "TestAccount";

			// Act
			await importer.ParseActivities(Filename, holdingsAndAccountsCollection.Object, accountName);

			// Assert
			holdingsAndAccountsCollection.Verify(x => x.AddPartialActivity(accountName, It.IsAny<IEnumerable<PartialActivity>>()), Times.Exactly(1));
		}
	}
}
