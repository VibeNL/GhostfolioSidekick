//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.GhostfolioAPI.Strategies;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Accounts;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Market;
//using GhostfolioSidekick.Model.Symbols;
//using Moq;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
//{
//	public class DeterminePriceTests
//	{
//		private readonly Mock<IMarketDataService> marketDataServiceMock;
//		private readonly DeterminePrice determinePrice;

//		public DeterminePriceTests()
//		{
//			marketDataServiceMock = new Mock<IMarketDataService>();
//			determinePrice = new DeterminePrice(marketDataServiceMock.Object);
//		}

//		[Fact]
//		public async Task Execute_ShouldSetUnitPrice_Receive_WhenUnitPriceIsZero()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Receive, DateTime.Now, 1, new Money(Currency.USD, 0), null),
//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = [new MarketData(new Money(Currency.USD, 5000), DateTime.Now)]
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0]!.UnitPrice!.Amount.Should().Be(5000);
//		}

//		[Fact]
//		public async Task Execute_ShouldSetUnitPrice_Send_WhenUnitPriceIsZero()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Send, DateTime.Now, 1, new Money(Currency.USD, 0), null),
//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = [new MarketData(new Money(Currency.USD, 5000), DateTime.Now)]
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0]!.UnitPrice!.Amount.Should().Be(5000);
//		}

//		[Fact]
//		public async Task Execute_ShouldSetUnitPrice_WhenUnitPriceIsNull()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Receive, DateTime.Now, 1, null, null),

//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = [new MarketData(new Money(Currency.USD, 5000), DateTime.Now)]
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0]!.UnitPrice!.Amount.Should().Be(5000);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotSetUnitPrice_WhenUnitPriceIsNotZero()
//		{
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Receive, DateTime.Now, 1, new Money(Currency.USD, 1000), null),

//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = [new MarketData(new Money(Currency.USD, 5000), DateTime.Now)]
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0].UnitPrice!.Amount.Should().Be(1000);
//		}

//		[Fact]
//		public async Task Execute_ShouldNotSetUnitPrice_WhenSymbolProfileIsNull()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var holding = new Holding(null)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Receive, DateTime.Now, 1, new Money(Currency.USD, 0), null),

//				]
//			};

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0].UnitPrice!.Amount.Should().Be(0);
//		}

//		[Fact]
//		public async Task Execute_NoActivities_WhenUnitPriceIsZero()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = [new MarketData(new Money(Currency.USD, 5000), DateTime.Now)]
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			// Implicit assertion that no exception is thrown
//		}

//		[Fact]
//		public async Task Execute_ShouldSetUnitPrice_NoPriceKnown_WhenUnitPriceIsZero()
//		{
//			// Arrange
//			var account = new Fixture().Create<Account>();
//			var symbolProfile = new SymbolProfile("BTC", "bitcoin", Currency.USD, "DataSource", AssetClass.Cash, AssetSubClass.CryptoCurrency, [], []);
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					new Activity(account, ActivityType.Receive, DateTime.Now, 1, new Money(Currency.USD, 0), null),
//				]
//			};

//			var marketDataProfile = new MarketDataProfile
//			{
//				AssetProfile = symbolProfile,
//				MarketData = []
//			};

//			marketDataServiceMock
//				.Setup(x => x.GetMarketData(It.IsAny<string>(), It.IsAny<string>()))
//				.ReturnsAsync(marketDataProfile);

//			// Act
//			await determinePrice.Execute(holding);

//			// Assert
//			holding.Activities[0]!.UnitPrice!.Amount.Should().Be(0);
//		}
//	}
//}
