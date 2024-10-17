//using FluentAssertions;
//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.ExternalDataProvider.Yahoo;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Symbols;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace ExternalDataProvider.UnitTests
//{
//	public class UnitTest1
//	{
//		private const string apiKey = "RE9XXn0JZkiD8DLjqePRk6zV7qF60cK1";

//		[Fact]
//		public async Task Test1()
//		{
//			// Arrange
//			var appSettingsMock = new Mock<IApplicationSettings>();

//			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
//			{
//				Settings = new Settings
//				{
//					DataProviderPolygonIOApiKey = apiKey
//				}
//			});

//			var x = new YahooRepository(new Mock<ILogger<YahooRepository>>().Object);

//			// Act
//			var r = (await x.GetCurrencyHistory(Currency.EUR, Currency.USD, DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2)))).ToList();

//			// Assert
//			Assert.NotNull(r);
//		}

//		[Fact]
//		public async Task MatchSymbol_Disney_Correct()
//		{
//			// Arrange
//			var appSettingsMock = new Mock<IApplicationSettings>();

//			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
//			{
//				Settings = new Settings
//				{
//					DataProviderPolygonIOApiKey = apiKey
//				}
//			});

//			var x = new YahooRepository(new Mock<ILogger<YahooRepository>>().Object);

//			// Act
//			var r = await x.MatchSymbol([
//				PartialSymbolIdentifier.CreateGeneric("US2546871060"), 
//				PartialSymbolIdentifier.CreateGeneric("DIS")]);

//			// Assert
//			r.Should().NotBeNull();
//			r!.Symbol.Should().Be("DIS");
//			r!.CountryWeight.Should().BeEquivalentTo([new CountryWeight("United States", string.Empty, string.Empty, 1)]);
//			r!.SectorWeights.Should().BeEquivalentTo([new SectorWeight("Communication Services", 1)]);
//			r!.AssetClass.Should().Be(AssetClass.Equity);
//			r!.AssetSubClass.Should().Be(AssetSubClass.Stock);
//		}

//		[Fact]
//		public async Task MatchSymbol_MSCIWorld_Correct()
//		{
//			// Arrange
//			var appSettingsMock = new Mock<IApplicationSettings>();

//			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
//			{
//				Settings = new Settings
//				{
//					DataProviderPolygonIOApiKey = apiKey
//				}
//			});

//			var x = new YahooRepository(new Mock<ILogger<YahooRepository>>().Object);

//			// Act
//			var r = await x.MatchSymbol([
//				PartialSymbolIdentifier.CreateStockBondAndETF("IE00BMC38736")]);

//			// Assert
//			r.Should().NotBeNull();
//			r!.Symbol.Should().Be("Y");
//			r!.CountryWeight.Should().BeEquivalentTo([new CountryWeight("WORLD", string.Empty, string.Empty, 1)]);
//			r!.SectorWeights.Should().BeEquivalentTo([new SectorWeight("ALL", 1)]);
//			r!.AssetClass.Should().Be(AssetClass.Equity);
//			r!.AssetSubClass.Should().Be(AssetSubClass.Stock);
//		}
//	}
//}