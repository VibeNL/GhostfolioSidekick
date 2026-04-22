using AutoFixture;
using AwesomeAssertions;
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
		private readonly Fixture _fixture;
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

			_fixture.Customize<BuyActivityFee>(o => o.FromFactory(() => new BuyActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<BuyActivityTax>(o => o.FromFactory(() => new BuyActivityTax(new Money(Currency.USD, 100m))));
			_fixture.Customize<SellActivityFee>(o => o.FromFactory(() => new SellActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<SellActivityTax>(o => o.FromFactory(() => new SellActivityTax(new Money(Currency.USD, 100m))));
			_fixture.Customize<DividendActivityFee>(o => o.FromFactory(() => new DividendActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<DividendActivityTax>(o => o.FromFactory(() => new DividendActivityTax(new Money(Currency.USD, 100m))));
			_fixture.Customize<ReceiveActivityFee>(o => o.FromFactory(() => new ReceiveActivityFee(new Money(Currency.USD, 100m))));
			_fixture.Customize<SellActivityFee>(o => o.FromFactory(() => new SellActivityFee(new Money(Currency.USD, 100m))));

			_exchangeRateServiceMock = new Mock<ICurrencyExchange>();
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapBuySellActivityCorrectly()
		{
			// Arrange
			var buyActivity = _fixture.Build<BuyActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, buyActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Should().Be(symbolProfile);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(buyActivity.Date);
			result.Fee.Should().Be(600m); // Assuming fees and taxes are converted to 100 each
			result.FeeCurrency.Should().Be(symbolProfile.Currency);
			result.Quantity.Should().Be(Math.Abs(buyActivity.AdjustedQuantity));
			result.Type.Should().Be(buyActivity.AdjustedQuantity > 0 ? ActivityType.BUY : ActivityType.SELL);
			result.UnitPrice.Should().Be(100m);
			result.ReferenceCode.Should().Be(buyActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapSendAndReceiveActivityCorrectly()
		{
			// Arrange
			var sendAndReceiveActivity = _fixture.Build<ReceiveActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, sendAndReceiveActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Should().Be(symbolProfile);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(sendAndReceiveActivity.Date);
			result.Fee.Should().Be(0); // No fees
			result.FeeCurrency.Should().Be(symbolProfile.Currency);
			result.Quantity.Should().Be(Math.Abs(sendAndReceiveActivity.AdjustedQuantity));
			result.Type.Should().Be(sendAndReceiveActivity.AdjustedQuantity > 0 ? ActivityType.BUY : ActivityType.SELL);
			result.UnitPrice.Should().Be(100m);
			result.ReferenceCode.Should().Be(sendAndReceiveActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapDividendActivityCorrectly()
		{
			// Arrange
			var dividendActivity = _fixture.Build<DividendActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, dividendActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Should().Be(symbolProfile);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(dividendActivity.Date);
			result.Fee.Should().Be(600m); // Assuming fees and taxes are converted to 100 each
			result.FeeCurrency.Should().Be(symbolProfile.Currency);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.DIVIDEND);
			result.UnitPrice.Should().Be(100m);
			result.ReferenceCode.Should().Be(dividendActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapInterestActivityCorrectly()
		{
			// Arrange
			var interestActivity = _fixture.Build<InterestActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, interestActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.Should().Be(interestActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.Should().Be(interestActivity.Description);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(interestActivity.Date);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.INTEREST);
			result.UnitPrice.Should().Be(interestActivity.Amount.Amount);
			result.ReferenceCode.Should().Be(interestActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapFeeActivityCorrectly()
		{
			// Arrange
			var feeActivity = _fixture.Build<FeeActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, feeActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.Should().Be(feeActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.Should().Be(feeActivity.Description);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(feeActivity.Date);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.FEE);
			result.UnitPrice.Should().Be(feeActivity.Amount.Amount);
			result.ReferenceCode.Should().Be(feeActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapValuableActivityCorrectly()
		{
			// Arrange
			var valuableActivity = _fixture.Build<ValuableActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, valuableActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.Should().Be(valuableActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.Should().Be(valuableActivity.Description);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(valuableActivity.Date);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.ITEM);
			result.UnitPrice.Should().Be(valuableActivity.Amount.Amount);
			result.ReferenceCode.Should().Be(valuableActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapLiabilityActivityCorrectly()
		{
			// Arrange
			var liabilityActivity = _fixture.Build<LiabilityActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, liabilityActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.Should().Be(liabilityActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.Should().Be(liabilityActivity.Description);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(liabilityActivity.Date);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.LIABILITY);
			result.UnitPrice.Should().Be(liabilityActivity.Amount.Amount);
			result.ReferenceCode.Should().Be(liabilityActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldReturnNullForUnsupportedActivity()
		{
			// Arrange
			var unsupportedActivity = _fixture.Build<KnownBalanceActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, unsupportedActivity, account);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapSendActivityCorrectly()
		{
			// Arrange
         var sendActivity = _fixture.Build<SendActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var symbolProfile = _fixture.Create<SymbolProfile>();
			var account = _fixture.Create<Account>();

			// Explicitly add 3 fees to match test expectation
			for (int i = 0; i < 3; i++)
			{
				sendActivity.Fees.Add(new SendActivityFee(new Money(Currency.USD, 100m)));
			}

			_exchangeRateServiceMock
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync(new Money(Currency.GetCurrency(symbolProfile.Currency), 100m));

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, symbolProfile, sendActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Should().Be(symbolProfile);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(sendActivity.Date);
           result.Fee.Should().Be(300m);
			result.FeeCurrency.Should().Be(symbolProfile.Currency);
			result.Quantity.Should().Be(Math.Abs(sendActivity.AdjustedQuantity));
			result.Type.Should().Be(ActivityType.SELL);
			result.UnitPrice.Should().Be(100m);
			result.ReferenceCode.Should().Be(sendActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldMapCorrectionActivityCorrectly()
		{
			// Arrange
			var correctionActivity = _fixture.Build<CorrectionActivity>().Without(x => x.Holding).Without(x => x.Account).Create();
			var account = _fixture.Create<Account>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, correctionActivity, account);

			// Assert
			result.Should().NotBeNull();
			result!.SymbolProfile.Currency.Should().Be(correctionActivity.Amount.Currency.Symbol);
			result.SymbolProfile.Name.Should().Be(correctionActivity.Description);
			result.Comment.Should().NotBeNull();
			result.Date.Should().Be(correctionActivity.Date);
			result.Quantity.Should().Be(1);
			result.Type.Should().Be(ActivityType.FEE);
			result.UnitPrice.Should().Be(correctionActivity.Amount.Amount);
			result.ReferenceCode.Should().Be(correctionActivity.TransactionId);
			result.AccountId.Should().Be(account.Id);
		}

        [Fact]
		public async Task ConvertToGhostfolioActivity_ShouldReturnNullForOtherUnsupportedActivities()
		{
			var modelAccount = new GhostfolioSidekick.Model.Accounts.Account("test");
			var holding = (Holding?)null;
			var date = DateTime.UtcNow;
			var money = new Money(Currency.USD, 1m);
			var transactionId = "txn";
			int? sortingPriority = null;
			string? description = null;

			var cashDeposit = new GhostfolioSidekick.Model.Activities.Types.CashDepositActivity(modelAccount, holding, date, money, transactionId, sortingPriority, description);
			var cashWithdrawal = new GhostfolioSidekick.Model.Activities.Types.CashWithdrawalActivity(modelAccount, holding, date, money, transactionId, sortingPriority, description);
            var partialSymbol = new GhostfolioSidekick.Model.Activities.PartialSymbolIdentifier(
				GhostfolioSidekick.Model.Activities.IdentifierType.ISIN, "ABC", null, new List<GhostfolioSidekick.Model.Activities.AssetClass>(), new List<GhostfolioSidekick.Model.Activities.AssetSubClass>());
			var stakingReward = new GhostfolioSidekick.Model.Activities.Types.StakingRewardActivity(modelAccount, holding, new List<GhostfolioSidekick.Model.Activities.PartialSymbolIdentifier> { partialSymbol }, date, 1m, transactionId, sortingPriority, description);

			var result1 = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, cashDeposit, null);
			var result2 = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, cashWithdrawal, null);
			var result3 = await ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, stakingReward, null);

			result1.Should().BeNull();
			result2.Should().BeNull();
			result3.Should().BeNull();
		}

      private record UnknownActivity : GhostfolioSidekick.Model.Activities.Activity
		{
			public UnknownActivity() : base(new GhostfolioSidekick.Model.Accounts.Account("unknown"), null, DateTime.UtcNow, "unknown", null, null) { }
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_ShouldThrowForUnknownActivityType()
		{
			// Arrange
			var unknownActivity = new UnknownActivity();
			var account = _fixture.Create<Account>();

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() =>
				ModelToContractMapper.ConvertToGhostfolioActivity(_exchangeRateServiceMock.Object, null, unknownActivity, account));
		}
	}
}
