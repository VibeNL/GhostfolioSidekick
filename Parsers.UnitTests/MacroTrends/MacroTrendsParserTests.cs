using FluentAssertions;
using GhostfolioSidekick.Parsers.MacroTrends;

namespace GhostfolioSidekick.Parsers.UnitTests.Bunq
{
	public class MacroTrendsParserTests
	{
		readonly MacroTrendsParser parser;

		public MacroTrendsParserTests()
		{
			parser = new MacroTrendsParser();
		}

		[Fact]
		public async Task CanParseHistoricData_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./MacroTrends/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ParseHistoricData_ATVI_Converted()
		{
			// Arrange

			// Act
			var result = await parser.ParseHistoricData("./MacroTrends/MacroTrends_Data_Download_ATVI.csv");

			// Assert
			result.Should().HaveCount(7546);
			result.Should().ContainSingle(x => x.Date == new DateTime(1993, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc) && x.Close == 0.8224M && x.Symbol == "ATVI");
		}
	}
}