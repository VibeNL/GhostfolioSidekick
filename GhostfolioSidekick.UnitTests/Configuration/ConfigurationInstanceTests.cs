using FluentAssertions;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.UnitTests.Configuration
{
	public class ConfigurationInstanceTests
	{
		[Fact]
		public void Parse_Example1()
		{
			// Arrange

			// Act
			var config = ConfigurationInstance.Parse(Example1);

			// Assert
			config.Mappings.Should().BeEquivalentTo(new[] {
				new Mapping{ MappingType = MappingType.Currency, Source = "GBX", Target = "GBp" },
				new Mapping{ MappingType = MappingType.Symbol, Source = "IE00077FRP95", Target = "SDIV.L" }
			});
			config.Symbols.Should().BeEquivalentTo(new[] {
				new SymbolConfiguration{ Symbol="SDIV.L", TrackingInsightSymbol="XLON:SDIV" },
			});
		}

		private string Example1 =
		@"
			{
	""mappings"":[
		{ ""type"":""currency"", ""source"":""GBX"", ""target"":""GBp""},
		{ ""type"":""symbol"", ""source"":""IE00077FRP95"", ""target"":""SDIV.L""}
	],
	""symbols"":[
		{ ""symbol"": ""SDIV.L"", ""trackinsight"": ""XLON:SDIV"" }
	]
}
	";
	}
}
