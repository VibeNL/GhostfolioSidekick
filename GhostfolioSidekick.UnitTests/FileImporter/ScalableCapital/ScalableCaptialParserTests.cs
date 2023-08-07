using FluentAssertions;
using GhostfolioSidekick.FileImporter.DeGiro;
using GhostfolioSidekick.FileImporter.ScalableCaptial;
using GhostfolioSidekick.Ghostfolio.API;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter.ScalableCapital
{
	public class ScalableCaptialParserTests
	{
		readonly Mock<IGhostfolioAPI> api;

		public ScalableCaptialParserTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task CanConvertOrders_WUMExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders("./FileImporter/ScalableCapital/WUMExample1.csv");

			// Assert
			canParse.Should().BeTrue();
		}

		[Fact]
		public async Task CanConvertOrders_RKKExample1_True()
		{
			// Arrange
			var parser = new ScalableCapitalParser(api.Object);

			// Act
			var canParse = await parser.CanConvertOrders("./FileImporter/ScalableCapital/RKKExample1.csv");

			// Assert
			canParse.Should().BeTrue();
		}
	}
}
