using AutoFixture;
using AutoFixture.Kernel;
using Shouldly;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ModelToContractMapperTests
	{
		private readonly IFixture _fixture;
		private readonly Mock<ICurrencyExchange> _exchangeRateServiceMock;

		public ModelToContractMapperTests()
		{
			_fixture = new Fixture();
			_fixture.Behaviors
							.OfType<ThrowingRecursionBehavior>()
							.ToList()
							.ForEach(b => _fixture.Behaviors.Remove(b));
			_fixture.Behaviors.Add(new OmitOnRecursionBehavior());

			_fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));
			_fixture.Customize<TimeOnly>(o => o.FromFactory((DateTime dt) => TimeOnly.FromDateTime(dt)));

			_fixture.Customize<BuySellActivityFee>(o => o.FromFactory(() => new BuySellActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<BuySellActivityTax>(o => o.FromFactory(() => new BuySellActivityTax(new Money(Currency.USD, 100m))));
			_fixture.Customize<DividendActivityFee>(o => o.FromFactory(() => new DividendActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<DividendActivityTax>(o => o.FromFactory(() => new DividendActivityTax(new Money(Currency.USD, 100m))));
			_fixture.Customize<SendAndReceiveActivityFee>(o => o.FromFactory(() => new SendAndReceiveActivityFee(new Money(Currency.USD, 100m))));

			_exchangeRateServiceMock = new Mock<ICurrencyExchange>();
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapBuySellActivityCorrectly()
		{
			// Arrange
			var buyActivity = _fixture.Build<BuySellActivity>().Without(x => x.Holding).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, buyActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.ShouldBe(symbolProfile);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(buyActivity.Date);
			result.Fee.ShouldBe(600m); // Assuming fees and taxes are converted to 100 each
			result.FeeCurrency.ShouldBe(symbolProfile.Currency);
			result.Quantity.ShouldBe(Math.Abs(buyActivity.AdjustedQuantity));
			result.Type.ShouldBe(buyActivity.AdjustedQuantity > 0 ? ActivityType.BUY : ActivityType.SELL);
			result.UnitPrice.ShouldBe(100m);
			result.ReferenceCode.ShouldBe(buyActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapSendAndReceiveActivityCorrectly()
		{
			// Arrange
			var sendAndReceiveActivity = _fixture.Build<SendAndReceiveActivity>().Without(x => x.Holding).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, sendAndReceiveActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.ShouldBe(symbolProfile);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(sendAndReceiveActivity.Date);
			result.Fee.ShouldBe(300m); // Assuming fees are converted to 100
			result.FeeCurrency.ShouldBe(symbolProfile.Currency);
			result.Quantity.ShouldBe(Math.Abs(sendAndReceiveActivity.AdjustedQuantity));
			result.Type.ShouldBe(sendAndReceiveActivity.AdjustedQuantity > 0 ? ActivityType.BUY : ActivityType.SELL);
			result.UnitPrice.ShouldBe(100m);
			result.ReferenceCode.ShouldBe(sendAndReceiveActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapDividendActivityCorrectly()
		{
			// Arrange
			var dividendActivity = _fixture.Build<DividendActivity>().Without(x => x.Holding).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, dividendActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.ShouldBe(symbolProfile);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(dividendActivity.Date);
			result.Fee.ShouldBe(600m); // Assuming fees and taxes are converted to 100 each
			result.FeeCurrency.ShouldBe(symbolProfile.Currency);
			result.Quantity.ShouldBe(1);
			result.Type.ShouldBe(ActivityType.DIVIDEND);
			result.UnitPrice.ShouldBe(100m);
			result.ReferenceCode.ShouldBe(dividendActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapInterestActivityCorrectly()
		{
			// Arrange
			var interestActivity = _fixture.Build<InterestActivity>().Without(x => x.Holding).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, interestActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.ShouldBe(interestActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.ShouldBe(interestActivity.Description);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(interestActivity.Date);
			result.Quantity.ShouldBe(1);
			result.Type.ShouldBe(ActivityType.INTEREST);
			result.UnitPrice.ShouldBe(interestActivity.Amount.Amount);
			result.ReferenceCode.ShouldBe(interestActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapFeeActivityCorrectly()
		{
			// Arrange
			var feeActivity = _fixture.Build<FeeActivity>().Without(x => x.Holding).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, feeActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.ShouldBe(feeActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.ShouldBe(feeActivity.Description);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(feeActivity.Date);
			result.Quantity.ShouldBe(1);
			result.Type.ShouldBe(ActivityType.FEE);
			result.UnitPrice.ShouldBe(feeActivity.Amount.Amount);
			result.ReferenceCode.ShouldBe(feeActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapValuableActivityCorrectly()
		{
			// Arrange
			var valuableActivity = _fixture.Build<ValuableActivity>().Without(x => x.Holding).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, valuableActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.ShouldBe(valuableActivity.Price.Currency.Symbol);
			result.SymbolProfile.Name.ShouldBe(valuableActivity.Description);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(valuableActivity.Date);
			result.Quantity.ShouldBe(1);
			result.Type.ShouldBe(ActivityType.ITEM);
			result.UnitPrice.ShouldBe(valuableActivity.Price.Amount);
			result.ReferenceCode.ShouldBe(valuableActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapLiabilityActivityCorrectly()
		{
			// Arrange
			var liabilityActivity = _fixture.Build<LiabilityActivity>().Without(x => x.Holding).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, liabilityActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.ShouldBe(liabilityActivity.Price.Currency.Symbol);
			result.SymbolProfile.Name.ShouldBe(liabilityActivity.Description);
			result.Comment.ShouldNotBeNull();
			result.Date.ShouldBe(liabilityActivity.Date);
			result.Quantity.ShouldBe(1);
			result.Type.ShouldBe(ActivityType.LIABILITY);
			result.UnitPrice.ShouldBe(liabilityActivity.Price.Amount);
			result.ReferenceCode.ShouldBe(liabilityActivity.TransactionId);
			result.AccountId.ShouldBe(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldReturnNullForUnsupportedActivity()
		{
			// Arrange
			var unsupportedActivity = _fixture.Build<KnownBalanceActivity>().Without(x => x.Holding).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, unsupportedActivity, account);

			// Assert
			result.Should().BeNull();
		}
	}
}
