using AwesomeAssertions;
using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class ActivityManagerTests
	{
		private readonly List<Account> _accounts;
		private readonly ActivityManager _activityManager;

		public ActivityManagerTests()
		{
			_accounts =
			[
				new Account { Name = "Account1", Id = 1 },
				new Account { Name = "Account2", Id = 2 }
			];
			_activityManager = new ActivityManager(_accounts);
		}

		[Fact]
		public async Task AddPartialActivity_ShouldAddPartialActivities()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];

			// Act
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Assert
			var activities = await _activityManager.GenerateActivities();
			_ = activities.Should().HaveCount(1);
		}

		[Fact]
		public async Task AddPartialActivity_ShouldAddToExistingList_WhenAccountAlreadyExists()
		{
			// Arrange
			List<PartialActivity> firstBatch =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];
			List<PartialActivity> secondBatch =
			[
				PartialActivity.CreateSell(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL2", null)], 5, new Money(Currency.USD, 20), new Money(Currency.USD, 100), "T2")
			];

			// Act
			_activityManager.AddPartialActivity("Account1", firstBatch);
			_activityManager.AddPartialActivity("Account1", secondBatch);

			// Assert
			var activities = await _activityManager.GenerateActivities();
			_ = activities.Should().HaveCount(2);
		}

		[Fact]
		public async Task GenerateActivities_ShouldGenerateActivities()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			_ = activities.First().Should().BeOfType<BuyActivity>();
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultiplePartialActivities()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 10, new Money(Currency.USD, 10), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			BuyActivity? activity = activities.First() as BuyActivity;
			_ = activity.Should().NotBeNull();
			_ = activity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldClearUnusedPartialActivities()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			_ = await _activityManager.GenerateActivities();

			// Assert
			var activities = await _activityManager.GenerateActivities();
			_ = activities.Should().BeEmpty();
		}

		[Fact]
		public async Task GenerateActivities_ShouldCreateNewAccount_WhenAccountNotFound()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];
			_activityManager.AddPartialActivity("NonExistentAccount", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			_ = activities.First().Account.Name.Should().Be("NonExistentAccount");
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultipleTaxes()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1"),
				PartialActivity.CreateTax(Currency.EUR, DateTime.Now, 3, new Money(Currency.EUR, 3), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			BuyActivity? activity = activities.First() as BuyActivity;
			_ = activity.Should().NotBeNull();
			_ = activity!.Taxes.Should().HaveCount(2);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultipleFeesAndTaxes()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateSell(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 2, new Money(Currency.USD, 2), "T1"),
				PartialActivity.CreateFee(Currency.EUR, DateTime.Now, 1, new Money(Currency.EUR, 1), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			SellActivity? activity = activities.First() as SellActivity;
			_ = activity.Should().NotBeNull();
			_ = activity!.Fees.Should().HaveCount(2);
			_ = activity.Taxes.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldCreateMultipleActivities_WhenOtherTransactionsExist()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateCashDeposit(Currency.USD, DateTime.Now, 50, new Money(Currency.USD, 50), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(2);
			_ = activities.Should().Contain(x => x is BuyActivity);
			_ = activities.Should().Contain(x => x is CashDepositActivity);

			// Check transaction IDs are different for additional activities
			var buyActivity = activities.OfType<BuyActivity>().First();
			var cashDepositActivity = activities.OfType<CashDepositActivity>().First();
			_ = buyActivity.TransactionId.Should().Be("T1");
			_ = cashDepositActivity.TransactionId.Should().Be("T1_2");
		}

		[Fact]
		public async Task GenerateActivities_ShouldUseTransactionWithSymbolIdentifiers_AsSource()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateCashDeposit(Currency.USD, DateTime.Now, 50, new Money(Currency.USD, 50), "T1"),
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(2);
			var buyActivity = activities.OfType<BuyActivity>().First();
			var cashDepositActivity = activities.OfType<CashDepositActivity>().First();

			// The buy activity (with symbol identifiers) should be the source, so it gets the original transaction ID
			_ = buyActivity.TransactionId.Should().Be("T1");
			_ = cashDepositActivity.TransactionId.Should().Be("T1_2");
		}

		// Test all activity types to ensure comprehensive coverage
		[Theory]
		[InlineData(PartialActivityType.Buy, typeof(BuyActivity))]
		[InlineData(PartialActivityType.Sell, typeof(SellActivity))]
		[InlineData(PartialActivityType.Receive, typeof(ReceiveActivity))]
		[InlineData(PartialActivityType.Send, typeof(SendActivity))]
		[InlineData(PartialActivityType.Dividend, typeof(DividendActivity))]
		[InlineData(PartialActivityType.Interest, typeof(InterestActivity))]
		[InlineData(PartialActivityType.Fee, typeof(FeeActivity))]
		[InlineData(PartialActivityType.CashDeposit, typeof(CashDepositActivity))]
		[InlineData(PartialActivityType.CashWithdrawal, typeof(CashWithdrawalActivity))]
		[InlineData(PartialActivityType.KnownBalance, typeof(KnownBalanceActivity))]
		[InlineData(PartialActivityType.Valuable, typeof(ValuableActivity))]
		[InlineData(PartialActivityType.Liability, typeof(LiabilityActivity))]
		[InlineData(PartialActivityType.GiftFiat, typeof(GiftFiatActivity))]
		[InlineData(PartialActivityType.GiftAsset, typeof(GiftAssetActivity))]
		[InlineData(PartialActivityType.StakingReward, typeof(StakingRewardActivity))]
		[InlineData(PartialActivityType.BondRepay, typeof(RepayBondActivity))]
		[InlineData(PartialActivityType.Correction, typeof(CorrectionActivity))]
		public async Task GenerateActivities_ShouldCreateCorrectActivityType(PartialActivityType activityType, Type expectedType)
		{
			// Arrange
			var partialActivity = CreatePartialActivityByType(activityType, DateTime.Now, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			_ = activities.First().Should().BeOfType(expectedType);
		}

		[Fact]
		public async Task GenerateActivities_ShouldThrowNotSupportedException_ForUnsupportedActivityType()
		{
			// Arrange
			PartialActivity partialActivity = new((PartialActivityType)999, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act & Assert
			_ = await Assert.ThrowsAsync<NotSupportedException>(_activityManager.GenerateActivities);
		}

		[Fact]
		public async Task GenerateActivities_ShouldSetCorrectProperties_ForBuyActivity()
		{
			// Arrange
			var date = DateTime.UtcNow; // Use UTC to avoid timezone issues
			PartialActivity partialActivity = PartialActivity.CreateBuy(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 5), new Money(Currency.USD, 50), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			BuyActivity? buyActivity = activities.First() as BuyActivity;
			_ = buyActivity.Should().NotBeNull();
			_ = buyActivity!.Date.Should().BeCloseTo(date, TimeSpan.FromMinutes(1));
			_ = buyActivity.Quantity.Should().Be(10);
			_ = buyActivity.UnitPrice.Amount.Should().Be(5);
			_ = buyActivity.UnitPrice.Currency.Should().Be(Currency.USD);
			_ = buyActivity.TransactionId.Should().Be("T1");
			_ = buyActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldSetCorrectProperties_ForDividendActivity()
		{
			// Arrange
			var date = DateTime.UtcNow; // Use UTC to avoid timezone issues
			PartialActivity partialActivity = PartialActivity.CreateDividend(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 100, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			DividendActivity? dividendActivity = activities.First() as DividendActivity;
			_ = dividendActivity.Should().NotBeNull();
			_ = dividendActivity!.Date.Should().BeCloseTo(date, TimeSpan.FromMinutes(1));
			_ = dividendActivity.Amount.Amount.Should().Be(100);
			_ = dividendActivity.TransactionId.Should().Be("T1");
			_ = dividendActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldCalculateCorrectTotalTransactionAmount_WhenNotProvided()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 5, new Money(Currency.USD, 20), new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			BuyActivity? buyActivity = activities.First() as BuyActivity;
			_ = buyActivity.Should().NotBeNull();
			_ = buyActivity!.UnitPrice.Amount.Should().Be(20);
			_ = buyActivity.Quantity.Should().Be(5);
		}

		[Fact]
		public async Task GenerateActivities_ShouldRemoveDuplicateSymbolIdentifiers()
		{
			// Arrange - Create a custom partial activity with duplicate symbol identifiers
			List<PartialSymbolIdentifier?> duplicateSymbolIds =
			[
				PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null),
				PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null) // Duplicate
			];
			PartialActivity partialActivity = PartialActivity.CreateBuy(Currency.USD, DateTime.Now, duplicateSymbolIds, 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			BuyActivity? buyActivity = activities.First() as BuyActivity;
			_ = buyActivity.Should().NotBeNull();
			_ = buyActivity!.PartialSymbolIdentifiers.Should().HaveCount(1); // Duplicates removed
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleReceiveActivity_WithFees()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateReceive(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			ReceiveActivity? receiveActivity = activities.First() as ReceiveActivity;
			_ = receiveActivity.Should().NotBeNull();
			_ = receiveActivity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleSendActivity_WithFees()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateSend(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 3, new Money(Currency.USD, 3), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			SendActivity? sendActivity = activities.First() as SendActivity;
			_ = sendActivity.Should().NotBeNull();
			_ = sendActivity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleDividendActivity_WithFeesAndTaxes()
		{
			// Arrange
			List<PartialActivity> partialActivities =
			[
				PartialActivity.CreateDividend(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 100, new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 2, new Money(Currency.USD, 2), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 15, new Money(Currency.USD, 15), "T1")
			];
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			_ = activities.Should().HaveCount(1);
			DividendActivity? dividendActivity = activities.First() as DividendActivity;
			_ = dividendActivity.Should().NotBeNull();
			_ = dividendActivity!.Fees.Should().HaveCount(1);
			_ = dividendActivity.Taxes.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleKnownBalanceActivity_WithCorrectCalculation()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateKnownBalance(Currency.USD, DateTime.Now, 100);
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			KnownBalanceActivity? knownBalanceActivity = activities.First() as KnownBalanceActivity;
			_ = knownBalanceActivity.Should().NotBeNull();
			_ = knownBalanceActivity!.Amount.Amount.Should().Be(100); // Uses the amount directly
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleGiftFiatActivity_WithCorrectCalculation()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateGift(Currency.USD, DateTime.Now, 100, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			GiftFiatActivity? giftFiatActivity = activities.First() as GiftFiatActivity;
			_ = giftFiatActivity.Should().NotBeNull();
			_ = giftFiatActivity!.Amount.Amount.Should().Be(100); // Uses money.Times(amount)
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleGiftAssetActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateGift(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			GiftAssetActivity? giftAssetActivity = activities.First() as GiftAssetActivity;
			_ = giftAssetActivity.Should().NotBeNull();
			_ = giftAssetActivity!.Quantity.Should().Be(10);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleStakingRewardActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateStakingReward(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 5, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			StakingRewardActivity? stakingRewardActivity = activities.First() as StakingRewardActivity;
			_ = stakingRewardActivity.Should().NotBeNull();
			_ = stakingRewardActivity!.Quantity.Should().Be(5);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleValuableActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateValuable(Currency.USD, DateTime.Now, "Test valuable", new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			ValuableActivity? valuableActivity = activities.First() as ValuableActivity;
			_ = valuableActivity.Should().NotBeNull();
			_ = valuableActivity!.Amount.Amount.Should().Be(1000);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleLiabilityActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateLiability(Currency.USD, DateTime.Now, "Test liability", new Money(Currency.USD, 500), new Money(Currency.USD, 500), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			LiabilityActivity? liabilityActivity = activities.First() as LiabilityActivity;
			_ = liabilityActivity.Should().NotBeNull();
			_ = liabilityActivity!.Amount.Amount.Should().Be(500);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleBondRepayActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateBondRepay(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "BOND1", null)], new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			RepayBondActivity? bondRepayActivity = activities.First() as RepayBondActivity;
			_ = bondRepayActivity.Should().NotBeNull();
			_ = bondRepayActivity!.Amount.Amount.Should().Be(1000);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleInterestActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateInterest(Currency.USD, DateTime.Now, 50, "Interest payment", new Money(Currency.USD, 50), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			InterestActivity? interestActivity = activities.First() as InterestActivity;
			_ = interestActivity.Should().NotBeNull();
			_ = interestActivity!.Amount.Amount.Should().Be(50);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleCashWithdrawalActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateCashWithdrawal(Currency.USD, DateTime.Now, 200, new Money(Currency.USD, 200), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			CashWithdrawalActivity? cashWithdrawalActivity = activities.First() as CashWithdrawalActivity;
			_ = cashWithdrawalActivity.Should().NotBeNull();
			_ = cashWithdrawalActivity!.Amount.Amount.Should().Be(200);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleFeeActivity()
		{
			// Arrange
			PartialActivity partialActivity = PartialActivity.CreateFee(Currency.USD, DateTime.Now, 25, new Money(Currency.USD, 25), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			FeeActivity? feeActivity = activities.First() as FeeActivity;
			_ = feeActivity.Should().NotBeNull();
			_ = feeActivity!.Amount.Amount.Should().Be(25);
		}

		private static PartialActivity CreatePartialActivityByType(PartialActivityType activityType, DateTime date, string transactionId)
		{
			return activityType switch
			{
				PartialActivityType.Buy => PartialActivity.CreateBuy(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Sell => PartialActivity.CreateSell(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Receive => PartialActivity.CreateReceive(date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, transactionId),
				PartialActivityType.Send => PartialActivity.CreateSend(date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, transactionId),
				PartialActivityType.Dividend => PartialActivity.CreateDividend(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Interest => PartialActivity.CreateInterest(Currency.USD, date, 50, "Interest", new Money(Currency.USD, 50), transactionId),
				PartialActivityType.Fee => PartialActivity.CreateFee(Currency.USD, date, 5, new Money(Currency.USD, 5), transactionId),
				PartialActivityType.CashDeposit => PartialActivity.CreateCashDeposit(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.CashWithdrawal => PartialActivity.CreateCashWithdrawal(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.KnownBalance => PartialActivity.CreateKnownBalance(Currency.USD, date, 100),
				PartialActivityType.Valuable => PartialActivity.CreateValuable(Currency.USD, date, "Valuable", new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), transactionId),
				PartialActivityType.Liability => PartialActivity.CreateLiability(Currency.USD, date, "Liability", new Money(Currency.USD, 500), new Money(Currency.USD, 500), transactionId),
				PartialActivityType.GiftFiat => PartialActivity.CreateGift(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.GiftAsset => PartialActivity.CreateGift(date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 10, transactionId),
				PartialActivityType.StakingReward => PartialActivity.CreateStakingReward(date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SYMBOL1", null)], 5, transactionId),
				PartialActivityType.BondRepay => PartialActivity.CreateBondRepay(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "BOND1", null)], new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), transactionId),
				PartialActivityType.Correction => PartialActivity.CreateCorrection(Currency.USD, date, 50, new Money(Currency.USD, 50), transactionId, "ABC"),
				_ => throw new NotSupportedException($"Activity type {activityType} not supported in test helper")
			};
		}
	}
}
