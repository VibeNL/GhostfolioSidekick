using FluentAssertions;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.Configuration.UnitTests
{
	public class ConfigurationInstanceTests
	{
		[Fact]
		public void Parse_NoManualSymbol_ParsedCorrectly()
		{
			// Arrange

			// Act
			var config = ConfigurationInstance.Parse(MappingsAndSymbols);

			// Assert
			config!.Mappings.Should().BeEquivalentTo(new[] {
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
			var config = ConfigurationInstance.Parse(ManualSymbol);

			// Assert
			config!.Symbols.Should().BeEquivalentTo(new[] {
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

		[Fact]
		public void Parse_AccountsAndPlatforms_ParsedCorrectly()
		{
			// Arrange

			// Act
			var config = ConfigurationInstance.Parse(AccountsAndPlatforms);

			// Assert
			config!.Platforms.Should().BeEquivalentTo(new[] {
				new PlatformConfiguration{
					Name = "Platform1",
					Url = "someurl"
				}
			});
			config.Accounts.Should().BeEquivalentTo(new[] {
				new AccountConfiguration{
					Name = "Account1",
					Comment = "SomeComment",
					Currency = "EUR",
					Platform = "Platform1"
				},
				new AccountConfiguration{
					Name = "Account2",
					Comment = null,
					Currency = "USD",
					Platform = null
				}
			});
		}

		private string MappingsAndSymbols =
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

		private string ManualSymbol =
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

		private string AccountsAndPlatforms =
		@"
			{
			""platforms"":[
				{ ""name"": ""Platform1"", ""url"": ""someurl"" }
			],
			""accounts"":[
				{ ""name"": ""Account1"", ""comment"": ""SomeComment"", ""currency"":""EUR"", ""platform"":""Platform1"" },
				{ ""name"": ""Account2"", ""currency"":""USD"" }
			]
				}
			";
	}
}
