using Moq;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Activities.Types;
using AutoFixture;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
{
	public class DeterminePriceTests
	{
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly DeterminePrice determinePrice;

		public DeterminePriceTests()
		{
			marketDataServiceMock = new Mock<IMarketDataService>();
			determinePrice = new DeterminePrice(marketDataServiceMock.Object);
		}

		[Fact]
		public async Task Execute_ShouldCalculateUnitPrice_WhenHoldingHasActivities()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create();
			var holding = new Holding(symbolProfile)
			{
				Activities = new List<IActivity>
				{
					new SendAndReceiveActivity(null, DateTime.Now, 1, string.Empty),
					new GiftActivity(null, DateTime.Now, 1, string.Empty),
				}
			};

			var marketDataProfile = new MarketDataProfile
			{
				AssetProfile = symbolProfile,
				MarketData = [new MarketData(new Money(Currency.USD, 100), DateTime.Now)]
			};

			marketDataServiceMock
				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
				.ReturnsAsync(marketDataProfile);

			// Act
			await determinePrice.Execute(holding);

			// Assert
			foreach (var activity in holding.Activities)
			{
				switch (activity)
				{
					case SendAndReceiveActivity sendAndReceiveActivity:
						sendAndReceiveActivity.CalculatedUnitPrice!.Amount.Should().Be(100);
						break;
					case GiftActivity giftActivity:
						giftActivity.CalculatedUnitPrice!.Amount.Should().Be(100);
						break;
				}
			}
		}

		[Fact]
		public async Task Execute_ShouldNotCalculateUnitPrice_WhenHoldingHasNoActivities()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create();
			var holding = new Holding(symbolProfile)
			{
				Activities = []
			};

			// Act
			await determinePrice.Execute(holding);

			// Assert
			marketDataServiceMock.Verify(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public async Task Execute_ShouldNotCalculateUnitPrice_WhenHoldingIsNull()
		{
			// Arrange
			var holding = new Holding(null)
			{
				Activities = new List<IActivity>
				{
					new SendAndReceiveActivity(null, DateTime.Now, 1, string.Empty),
					new GiftActivity(null, DateTime.Now, 1, string.Empty),
				}
			};

			// Act
			await determinePrice.Execute(holding);

			// Assert
			marketDataServiceMock.Verify(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}
	}
}
