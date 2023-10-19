using FluentAssertions;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.UnitTests.Configuration
{
	public class ConfigurationInstanceTests
	{
		[Fact]
		public void Parse_NoManualSymbol_ParsedCorrectly()
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

		[Fact]
		public void Parse_OnlyManualSymbol_ParsedCorrectly()
		{
			// Arrange

			// Act
			var config = ConfigurationInstance.Parse(Example2);

			// Assert
			config.Symbols.Should().BeEquivalentTo(new[] {
				new SymbolConfiguration{
					Symbol="Manual1",
					ManualSymbolConfiguration = new ManualSymbolConfiguration{
					 AssetClass = "Equity",
					 AssetSubClass = "Stock",
					 Currency = "EUR",
					 ISIN = "QWERTY",
					 Name = "TESTSymbol",
					} },
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

		private string Example2 =
		@"
			{
			""symbols"":[
				{ ""symbol"": ""Manual1"", ""manualSymbolConfiguration"": 
					{
						""currency"":""EUR"",
						""isin"":""QWERTY"",
						""name"":""TESTSymbol"",
						""assetSubClass"":""Stock"",
						""assetClass"":""Equity""
					} 
				}
			]
				}
			";
	}
}
