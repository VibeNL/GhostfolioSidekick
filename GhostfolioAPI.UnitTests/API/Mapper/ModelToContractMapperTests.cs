using Moq;
using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;
using AutoFixture.Kernel;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ModelToContractMapperTests
	{
		private const string ManualDataSource = "MANUAL";
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;

		public ModelToContractMapperTests()
		{
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency?>(), It.IsAny<Currency?>(), It.IsAny<DateTime>())).ReturnsAsync(1);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_BuySellActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<BuySellActivity>();
			
			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(activity.Quantity);
			result.UnitPrice.Should().Be(activity.UnitPrice!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_BuySellActivityWithoutUnitPrice_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<BuySellActivity>();
			activity.UnitPrice = null;

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(activity.Quantity);
			result.UnitPrice.Should().Be(0);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_DividendActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<DividendActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.DIVIDEND);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(activity.Amount!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_InterestActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<InterestActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.INTEREST);
			result.SymbolProfile!.Symbol.Should().Be(activity.Description);
			result.SymbolProfile.DataSource.Should().Be(ManualDataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(activity.Amount!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_FeeActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<FeeActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.FEE);
			result.SymbolProfile!.Symbol.Should().Be(activity.Description);
			result.SymbolProfile.DataSource.Should().Be(ManualDataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(activity.Amount!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ValuableActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<ValuableActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.ITEM);
			result.SymbolProfile!.Symbol.Should().Be(activity.Description);
			result.SymbolProfile.DataSource.Should().Be(ManualDataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(activity.Price!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_LiabilityActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<LiabilityActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.LIABILITY);
			result.SymbolProfile!.Symbol.Should().Be(activity.Description);
			result.SymbolProfile.DataSource.Should().Be(ManualDataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(activity.Price!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_GiftActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<GiftActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(activity.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_GiftActivityWithoutSymbol_Success()
		{
			// Arrange
			var activity = DefaultFixture.Create().Create<GiftActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, null, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(activity.Description);
			result.SymbolProfile.DataSource.Should().Be(ManualDataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(activity.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_SendAndReceiveActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<SendAndReceiveActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(activity.Quantity);
			result.UnitPrice.Should().Be(activity.UnitPrice!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_SendAndReceiveActivity_Send_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = DefaultFixture.Create().Create<SendAndReceiveActivity>();
			activity.Quantity = -activity.Quantity;

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.SELL);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(activity.Date);
			result.Quantity.Should().Be(-activity.Quantity);
			result.UnitPrice.Should().Be(activity.UnitPrice!.Amount);
			// TODO check more
		}

		[Theory]
		[InlineData(typeof(KnownBalanceActivity))]
		[InlineData(typeof(CashDepositWithdrawalActivity))]
		[InlineData(typeof(StockSplitActivity))]
		[InlineData(typeof(StakingRewardActivity))]
		public async Task ConvertToGhostfolioActivity_Ignored_Success(Type type)
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var activity = (IActivity)DefaultFixture.Create().Create(type, new SpecimenContext(DefaultFixture.Create()));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, activity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.IGNORE);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_UnknownActivityType_ThrowsException()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var unknownActivity = new Mock<IActivity>().Object;

			// Act
			Func<Task> act = async () => await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, unknownActivity);

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}
	}
}
