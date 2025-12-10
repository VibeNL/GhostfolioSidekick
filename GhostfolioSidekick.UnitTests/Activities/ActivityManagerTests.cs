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
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};

			// Act
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Assert
			var activities = await _activityManager.GenerateActivities();
			activities.Should().HaveCount(1);
		}

		[Fact]
		public async Task AddPartialActivity_ShouldAddToExistingList_WhenAccountAlreadyExists()
		{
			// Arrange
			var firstBatch = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};
			var secondBatch = new List<PartialActivity>
			{
				PartialActivity.CreateSell(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL2")], 5, new Money(Currency.USD, 20), new Money(Currency.USD, 100), "T2")
			};

			// Act
			_activityManager.AddPartialActivity("Account1", firstBatch);
			_activityManager.AddPartialActivity("Account1", secondBatch);

			// Assert
			var activities = await _activityManager.GenerateActivities();
			activities.Should().HaveCount(2);
		}

		[Fact]
		public async Task GenerateActivities_ShouldGenerateActivities()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			activities.First().Should().BeOfType<BuyActivity>();
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultiplePartialActivities()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 10, new Money(Currency.USD, 10), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var activity = activities.First() as BuyActivity;
			activity.Should().NotBeNull();
			activity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldClearUnusedPartialActivities()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			await _activityManager.GenerateActivities();

			// Assert
			var activities = await _activityManager.GenerateActivities();
			activities.Should().BeEmpty();
		}

		[Fact]
		public async Task GenerateActivities_ShouldCreateNewAccount_WhenAccountNotFound()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};
			_activityManager.AddPartialActivity("NonExistentAccount", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			activities.First().Account.Name.Should().Be("NonExistentAccount");
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultipleTaxes()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1"),
				PartialActivity.CreateTax(Currency.EUR, DateTime.Now, 3, new Money(Currency.EUR, 3), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var activity = activities.First() as BuyActivity;
			activity.Should().NotBeNull();
			activity!.Taxes.Should().HaveCount(2);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleMultipleFeesAndTaxes()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateSell(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 2, new Money(Currency.USD, 2), "T1"),
				PartialActivity.CreateFee(Currency.EUR, DateTime.Now, 1, new Money(Currency.EUR, 1), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var activity = activities.First() as SellActivity;
			activity.Should().NotBeNull();
			activity!.Fees.Should().HaveCount(2);
			activity.Taxes.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldCreateMultipleActivities_WhenOtherTransactionsExist()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateCashDeposit(Currency.USD, DateTime.Now, 50, new Money(Currency.USD, 50), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(2);
			activities.Should().Contain(x => x is BuyActivity);
			activities.Should().Contain(x => x is CashDepositActivity);

			// Check transaction IDs are different for additional activities
			var buyActivity = activities.OfType<BuyActivity>().First();
			var cashDepositActivity = activities.OfType<CashDepositActivity>().First();
			buyActivity.TransactionId.Should().Be("T1");
			cashDepositActivity.TransactionId.Should().Be("T1_2");
		}

		[Fact]
		public async Task GenerateActivities_ShouldUseTransactionWithSymbolIdentifiers_AsSource()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateCashDeposit(Currency.USD, DateTime.Now, 50, new Money(Currency.USD, 50), "T1"),
				PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(2);
			var buyActivity = activities.OfType<BuyActivity>().First();
			var cashDepositActivity = activities.OfType<CashDepositActivity>().First();

			// The buy activity (with symbol identifiers) should be the source, so it gets the original transaction ID
			buyActivity.TransactionId.Should().Be("T1");
			cashDepositActivity.TransactionId.Should().Be("T1_2");
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
		public async Task GenerateActivities_ShouldCreateCorrectActivityType(PartialActivityType activityType, Type expectedType)
		{
			// Arrange
			var partialActivity = CreatePartialActivityByType(activityType, DateTime.Now, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			activities.First().Should().BeOfType(expectedType);
		}

		[Fact]
		public async Task GenerateActivities_ShouldThrowNotSupportedException_ForUnsupportedActivityType()
		{
			// Arrange
			var partialActivity = new PartialActivity((PartialActivityType)999, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() => _activityManager.GenerateActivities());
		}

		[Fact]
		public async Task GenerateActivities_ShouldSetCorrectProperties_ForBuyActivity()
		{
			// Arrange
			var date = DateTime.UtcNow; // Use UTC to avoid timezone issues
			var partialActivity = PartialActivity.CreateBuy(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 5), new Money(Currency.USD, 50), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var buyActivity = activities.First() as BuyActivity;
			buyActivity.Should().NotBeNull();
			buyActivity!.Date.Should().BeCloseTo(date, TimeSpan.FromMinutes(1));
			buyActivity.Quantity.Should().Be(10);
			buyActivity.UnitPrice.Amount.Should().Be(5);
			buyActivity.UnitPrice.Currency.Should().Be(Currency.USD);
			buyActivity.TransactionId.Should().Be("T1");
			buyActivity.TotalTransactionAmount.Amount.Should().Be(50);
			buyActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldSetCorrectProperties_ForDividendActivity()
		{
			// Arrange
			var date = DateTime.UtcNow; // Use UTC to avoid timezone issues
			var partialActivity = PartialActivity.CreateDividend(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 100, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var dividendActivity = activities.First() as DividendActivity;
			dividendActivity.Should().NotBeNull();
			dividendActivity!.Date.Should().BeCloseTo(date, TimeSpan.FromMinutes(1));
			dividendActivity.Amount.Amount.Should().Be(100);
			dividendActivity.TransactionId.Should().Be("T1");
			dividendActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldCalculateCorrectTotalTransactionAmount_WhenNotProvided()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateBuy(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 5, new Money(Currency.USD, 20), new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var buyActivity = activities.First() as BuyActivity;
			buyActivity.Should().NotBeNull();
			buyActivity!.TotalTransactionAmount.Amount.Should().Be(100); // 5 * 20
		}

		[Fact]
		public async Task GenerateActivities_ShouldRemoveDuplicateSymbolIdentifiers()
		{
			// Arrange - Create a custom partial activity with duplicate symbol identifiers
			var duplicateSymbolIds = new List<PartialSymbolIdentifier>
			{
				PartialSymbolIdentifier.CreateGeneric("SYMBOL1"),
				PartialSymbolIdentifier.CreateGeneric("SYMBOL1") // Duplicate
			};
			var partialActivity = PartialActivity.CreateBuy(Currency.USD, DateTime.Now, duplicateSymbolIds, 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var buyActivity = activities.First() as BuyActivity;
			buyActivity.Should().NotBeNull();
			buyActivity!.PartialSymbolIdentifiers.Should().HaveCount(1); // Duplicates removed
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleReceiveActivity_WithFees()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateReceive(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 5, new Money(Currency.USD, 5), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var receiveActivity = activities.First() as ReceiveActivity;
			receiveActivity.Should().NotBeNull();
			receiveActivity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleSendActivity_WithFees()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateSend(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 3, new Money(Currency.USD, 3), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var sendActivity = activities.First() as SendActivity;
			sendActivity.Should().NotBeNull();
			sendActivity!.Fees.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleDividendActivity_WithFeesAndTaxes()
		{
			// Arrange
			var partialActivities = new List<PartialActivity>
			{
				PartialActivity.CreateDividend(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 100, new Money(Currency.USD, 100), "T1"),
				PartialActivity.CreateFee(Currency.USD, DateTime.Now, 2, new Money(Currency.USD, 2), "T1"),
				PartialActivity.CreateTax(Currency.USD, DateTime.Now, 15, new Money(Currency.USD, 15), "T1")
			};
			_activityManager.AddPartialActivity("Account1", partialActivities);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			activities.Should().HaveCount(1);
			var dividendActivity = activities.First() as DividendActivity;
			dividendActivity.Should().NotBeNull();
			dividendActivity!.Fees.Should().HaveCount(1);
			dividendActivity.Taxes.Should().HaveCount(1);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleKnownBalanceActivity_WithCorrectCalculation()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateKnownBalance(Currency.USD, DateTime.Now, 100);
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var knownBalanceActivity = activities.First() as KnownBalanceActivity;
			knownBalanceActivity.Should().NotBeNull();
			knownBalanceActivity!.Amount.Amount.Should().Be(100); // Uses the amount directly
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleGiftFiatActivity_WithCorrectCalculation()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateGift(Currency.USD, DateTime.Now, 100, new Money(Currency.USD, 100), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var giftFiatActivity = activities.First() as GiftFiatActivity;
			giftFiatActivity.Should().NotBeNull();
			giftFiatActivity!.Amount.Amount.Should().Be(100); // Uses money.Times(amount)
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleGiftAssetActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateGift(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var giftAssetActivity = activities.First() as GiftAssetActivity;
			giftAssetActivity.Should().NotBeNull();
			giftAssetActivity!.Quantity.Should().Be(10);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleStakingRewardActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateStakingReward(DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 5, "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var stakingRewardActivity = activities.First() as StakingRewardActivity;
			stakingRewardActivity.Should().NotBeNull();
			stakingRewardActivity!.Quantity.Should().Be(5);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleValuableActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateValuable(Currency.USD, DateTime.Now, "Test valuable", new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var valuableActivity = activities.First() as ValuableActivity;
			valuableActivity.Should().NotBeNull();
			valuableActivity!.Amount.Amount.Should().Be(1000);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleLiabilityActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateLiability(Currency.USD, DateTime.Now, "Test liability", new Money(Currency.USD, 500), new Money(Currency.USD, 500), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var liabilityActivity = activities.First() as LiabilityActivity;
			liabilityActivity.Should().NotBeNull();
			liabilityActivity!.Amount.Amount.Should().Be(500);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleBondRepayActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateBondRepay(Currency.USD, DateTime.Now, [PartialSymbolIdentifier.CreateGeneric("BOND1")], new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var bondRepayActivity = activities.First() as RepayBondActivity;
			bondRepayActivity.Should().NotBeNull();
			bondRepayActivity!.Amount.Amount.Should().Be(1000);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleInterestActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateInterest(Currency.USD, DateTime.Now, 50, "Interest payment", new Money(Currency.USD, 50), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var interestActivity = activities.First() as InterestActivity;
			interestActivity.Should().NotBeNull();
			interestActivity!.Amount.Amount.Should().Be(50);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleCashWithdrawalActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateCashWithdrawal(Currency.USD, DateTime.Now, 200, new Money(Currency.USD, 200), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var cashWithdrawalActivity = activities.First() as CashWithdrawalActivity;
			cashWithdrawalActivity.Should().NotBeNull();
			cashWithdrawalActivity!.Amount.Amount.Should().Be(200);
		}

		[Fact]
		public async Task GenerateActivities_ShouldHandleFeeActivity()
		{
			// Arrange
			var partialActivity = PartialActivity.CreateFee(Currency.USD, DateTime.Now, 25, new Money(Currency.USD, 25), "T1");
			_activityManager.AddPartialActivity("Account1", [partialActivity]);

			// Act
			var activities = await _activityManager.GenerateActivities();

			// Assert
			var feeActivity = activities.First() as FeeActivity;
			feeActivity.Should().NotBeNull();
			feeActivity!.Amount.Amount.Should().Be(25);
		}

		private static PartialActivity CreatePartialActivityByType(PartialActivityType activityType, DateTime date, string transactionId)
		{
			return activityType switch
			{
				PartialActivityType.Buy => PartialActivity.CreateBuy(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Sell => PartialActivity.CreateSell(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, new Money(Currency.USD, 10), new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Receive => PartialActivity.CreateReceive(date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, transactionId),
				PartialActivityType.Send => PartialActivity.CreateSend(date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, transactionId),
				PartialActivityType.Dividend => PartialActivity.CreateDividend(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.Interest => PartialActivity.CreateInterest(Currency.USD, date, 50, "Interest", new Money(Currency.USD, 50), transactionId),
				PartialActivityType.Fee => PartialActivity.CreateFee(Currency.USD, date, 5, new Money(Currency.USD, 5), transactionId),
				PartialActivityType.CashDeposit => PartialActivity.CreateCashDeposit(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.CashWithdrawal => PartialActivity.CreateCashWithdrawal(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.KnownBalance => PartialActivity.CreateKnownBalance(Currency.USD, date, 100),
				PartialActivityType.Valuable => PartialActivity.CreateValuable(Currency.USD, date, "Valuable", new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), transactionId),
				PartialActivityType.Liability => PartialActivity.CreateLiability(Currency.USD, date, "Liability", new Money(Currency.USD, 500), new Money(Currency.USD, 500), transactionId),
				PartialActivityType.GiftFiat => PartialActivity.CreateGift(Currency.USD, date, 100, new Money(Currency.USD, 100), transactionId),
				PartialActivityType.GiftAsset => PartialActivity.CreateGift(date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 10, transactionId),
				PartialActivityType.StakingReward => PartialActivity.CreateStakingReward(date, [PartialSymbolIdentifier.CreateGeneric("SYMBOL1")], 5, transactionId),
				PartialActivityType.BondRepay => PartialActivity.CreateBondRepay(Currency.USD, date, [PartialSymbolIdentifier.CreateGeneric("BOND1")], new Money(Currency.USD, 1000), new Money(Currency.USD, 1000), transactionId),
				_ => throw new NotSupportedException($"Activity type {activityType} not supported in test helper")
			};
		}
	}
}
