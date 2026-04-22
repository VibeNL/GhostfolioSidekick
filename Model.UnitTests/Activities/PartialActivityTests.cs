using System;
using GhostfolioSidekick.Model.Activities;
using Xunit;

namespace GhostfolioSidekick.Model.UnitTests.Activities
{
    public class PartialActivityTests
    {
        private static readonly DateTime TestDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly List<PartialSymbolIdentifier?> EmptySymbols = [];

        // ── CreateCashDeposit ──────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashDeposit_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashDeposit(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx1"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashDeposit_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashDeposit(Currency.USD, TestDate, 1, new Money(Currency.USD, total), "tx1"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CreateCashDeposit_ShouldSucceed(decimal amount)
        {
            var activity = PartialActivity.CreateCashDeposit(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx1");
            Assert.Equal(amount, activity.Amount);
            Assert.Equal(PartialActivityType.CashDeposit, activity.ActivityType);
            Assert.Equal(Currency.USD, activity.Currency);
            Assert.Equal(TestDate, activity.Date);
        }

        // ── CreateCashWithdrawal ───────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashWithdrawal_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashWithdrawal(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx2"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashWithdrawal_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashWithdrawal(Currency.USD, TestDate, 1, new Money(Currency.USD, total), "tx2"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateCashWithdrawal_ShouldSucceed()
        {
            var activity = PartialActivity.CreateCashWithdrawal(Currency.EUR, TestDate, 50, new Money(Currency.EUR, 50), "tx2");
            Assert.Equal(50, activity.Amount);
            Assert.Equal(PartialActivityType.CashWithdrawal, activity.ActivityType);
        }

        // ── CreateGift (fiat) ──────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateGiftFiat_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateGift(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx5"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateGiftFiat_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateGift(Currency.USD, TestDate, 10, new Money(Currency.USD, total), "tx5"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateGiftFiat_ShouldSucceed()
        {
            var activity = PartialActivity.CreateGift(Currency.USD, TestDate, 10, new Money(Currency.USD, 10), "tx5");
            Assert.Equal(10, activity.Amount);
            Assert.Equal(PartialActivityType.GiftFiat, activity.ActivityType);
            Assert.Equal("Gift", activity.Description);
        }

        // ── CreateGift (asset) ─────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateGiftAsset_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateGift(TestDate, EmptySymbols, amount, "tx6"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateGiftAsset_ShouldSucceed()
        {
            var activity = PartialActivity.CreateGift(TestDate, EmptySymbols, 5, "tx6");
            Assert.Equal(5, activity.Amount);
            Assert.Equal(PartialActivityType.GiftAsset, activity.ActivityType);
        }

        // ── CreateInterest ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateInterest_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateInterest(Currency.USD, TestDate, amount, "Interest", new Money(Currency.USD, 1), "tx"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateInterest_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateInterest(Currency.USD, TestDate, 1, "Interest", new Money(Currency.USD, total), "tx"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateInterest_ShouldSucceed()
        {
            var activity = PartialActivity.CreateInterest(Currency.USD, TestDate, 3, "Monthly interest", new Money(Currency.USD, 3), "tx");
            Assert.Equal(3, activity.Amount);
            Assert.Equal(PartialActivityType.Interest, activity.ActivityType);
            Assert.Equal("Monthly interest", activity.Description);
        }

        // ── CreateKnownBalance ─────────────────────────────────────────────────

        [Fact]
        public void CreateKnownBalance_ShouldSucceed()
        {
            var activity = PartialActivity.CreateKnownBalance(Currency.USD, TestDate, 1000, 5);
            Assert.Equal(1000, activity.Amount);
            Assert.Equal(PartialActivityType.KnownBalance, activity.ActivityType);
            Assert.Equal(5, activity.SortingPriority);
        }

        [Fact]
        public void CreateKnownBalance_ShouldDefaultSortingPriorityToZero()
        {
            var activity = PartialActivity.CreateKnownBalance(Currency.USD, TestDate, 500);
            Assert.Equal(0, activity.SortingPriority);
        }

        // ── CreateTax ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateTax_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateTax(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateTax_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateTax(Currency.USD, TestDate, 1, new Money(Currency.USD, total), "tx"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateTax_ShouldSucceed()
        {
            var activity = PartialActivity.CreateTax(Currency.USD, TestDate, 20, new Money(Currency.USD, 20), "tx");
            Assert.Equal(20, activity.Amount);
            Assert.Equal(PartialActivityType.Tax, activity.ActivityType);
            Assert.Equal("Tax", activity.Description);
        }

        // ── CreateFee ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateFee_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateFee(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateFee_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateFee(Currency.USD, TestDate, 1, new Money(Currency.USD, total), "tx"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateFee_ShouldSucceed()
        {
            var activity = PartialActivity.CreateFee(Currency.USD, TestDate, 5, new Money(Currency.USD, 5), "tx");
            Assert.Equal(5, activity.Amount);
            Assert.Equal(PartialActivityType.Fee, activity.ActivityType);
            Assert.Equal("Fee", activity.Description);
        }

        // ── CreateBuy ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateBuy_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateBuy(Currency.USD, TestDate, EmptySymbols, amount, new Money(Currency.USD, 1), new Money(Currency.USD, 1), "tx3"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateBuy_ShouldThrow_OnNegativeUnitPrice(decimal unitPrice)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateBuy(Currency.USD, TestDate, EmptySymbols, 1, new Money(Currency.USD, unitPrice), new Money(Currency.USD, 1), "tx3"));
            Assert.Contains("UnitPrice cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateBuy_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateBuy(Currency.USD, TestDate, EmptySymbols, 1, new Money(Currency.USD, 10), new Money(Currency.USD, total), "tx3"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateBuy_ShouldSucceed()
        {
            var activity = PartialActivity.CreateBuy(Currency.USD, TestDate, EmptySymbols, 2, new Money(Currency.USD, 50), new Money(Currency.USD, 100), "tx3");
            Assert.Equal(2, activity.Amount);
            Assert.Equal(PartialActivityType.Buy, activity.ActivityType);
            Assert.Equal(50, activity.UnitPrice!.Amount);
        }

        // ── CreateSell ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSell_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSell(Currency.USD, TestDate, EmptySymbols, amount, new Money(Currency.USD, 1), new Money(Currency.USD, 1), "tx4"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSell_ShouldThrow_OnNegativeUnitPrice(decimal unitPrice)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSell(Currency.USD, TestDate, EmptySymbols, 1, new Money(Currency.USD, unitPrice), new Money(Currency.USD, 1), "tx4"));
            Assert.Contains("UnitPrice cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSell_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSell(Currency.USD, TestDate, EmptySymbols, 1, new Money(Currency.USD, 10), new Money(Currency.USD, total), "tx4"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateSell_ShouldSucceed()
        {
            var activity = PartialActivity.CreateSell(Currency.USD, TestDate, EmptySymbols, 3, new Money(Currency.USD, 40), new Money(Currency.USD, 120), "tx4");
            Assert.Equal(3, activity.Amount);
            Assert.Equal(PartialActivityType.Sell, activity.ActivityType);
        }

        // ── CreateDividend ────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateDividend_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateDividend(Currency.USD, TestDate, EmptySymbols, amount, new Money(Currency.USD, 1), "tx"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateDividend_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateDividend(Currency.USD, TestDate, EmptySymbols, 1, new Money(Currency.USD, total), "tx"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateDividend_ShouldSucceed()
        {
            var activity = PartialActivity.CreateDividend(Currency.USD, TestDate, EmptySymbols, 10, new Money(Currency.USD, 10), "tx");
            Assert.Equal(10, activity.Amount);
            Assert.Equal(PartialActivityType.Dividend, activity.ActivityType);
            Assert.Equal(1, activity.UnitPrice!.Amount);
            Assert.Equal(10, activity.TotalTransactionAmount.Amount);
        }

        // ── CreateCurrencyConvert ─────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCurrencyConvert_ShouldThrow_OnNegativeSource(decimal negative)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCurrencyConvert(TestDate, new Money(Currency.USD, negative), new Money(Currency.EUR, 1), new Money(Currency.USD, 1), "tx9").ToList());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCurrencyConvert_ShouldThrow_OnNegativeTarget(decimal negative)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCurrencyConvert(TestDate, new Money(Currency.USD, 1), new Money(Currency.EUR, negative), new Money(Currency.USD, 1), "tx10").ToList());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCurrencyConvert_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCurrencyConvert(TestDate, new Money(Currency.USD, 1), new Money(Currency.EUR, 1), new Money(Currency.USD, total), "tx").ToList());
        }

        [Fact]
        public void CreateCurrencyConvert_ShouldYieldTwoActivities()
        {
            var activities = PartialActivity.CreateCurrencyConvert(
                TestDate,
                new Money(Currency.USD, 100),
                new Money(Currency.EUR, 90),
                new Money(Currency.USD, 100),
                "tx").ToList();

            Assert.Equal(2, activities.Count);
            Assert.Equal(PartialActivityType.CashWithdrawal, activities[0].ActivityType);
            Assert.Equal(100, activities[0].Amount);
            Assert.Equal(Currency.USD, activities[0].Currency);
            Assert.Equal(PartialActivityType.CashDeposit, activities[1].ActivityType);
            Assert.Equal(90, activities[1].Amount);
            Assert.Equal(Currency.EUR, activities[1].Currency);
            Assert.Contains("[CurrencyConvertSource]", activities[0].TransactionId);
            Assert.Contains("[CurrencyConvertTarget]", activities[1].TransactionId);
        }

        // ── CreateStakingReward ───────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateStakingReward_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateStakingReward(TestDate, EmptySymbols, amount, "tx"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateStakingReward_ShouldSucceed()
        {
            var activity = PartialActivity.CreateStakingReward(TestDate, EmptySymbols, 7, "tx");
            Assert.Equal(7, activity.Amount);
            Assert.Equal(PartialActivityType.StakingReward, activity.ActivityType);
        }

        // ── CreateSend ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSend_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSend(TestDate, EmptySymbols, amount, "tx7"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateSend_ShouldSucceed()
        {
            var activity = PartialActivity.CreateSend(TestDate, EmptySymbols, 4, "tx7");
            Assert.Equal(4, activity.Amount);
            Assert.Equal(PartialActivityType.Send, activity.ActivityType);
        }

        // ── CreateReceive ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateReceive_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateReceive(TestDate, EmptySymbols, amount, "tx8"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateReceive_ShouldSucceed()
        {
            var activity = PartialActivity.CreateReceive(TestDate, EmptySymbols, 6, "tx8");
            Assert.Equal(6, activity.Amount);
            Assert.Equal(PartialActivityType.Receive, activity.ActivityType);
        }

        // ── CreateAssetConvert ────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateAssetConvert_ShouldThrow_OnNegativeSourceAmount(decimal negative)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateAssetConvert(TestDate, EmptySymbols, negative, EmptySymbols, 1, "tx11").ToList());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateAssetConvert_ShouldThrow_OnNegativeTargetAmount(decimal negative)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateAssetConvert(TestDate, EmptySymbols, 1, EmptySymbols, negative, "tx12").ToList());
        }

        [Fact]
        public void CreateAssetConvert_ShouldYieldTwoActivities()
        {
            var activities = PartialActivity.CreateAssetConvert(TestDate, EmptySymbols, 5, EmptySymbols, 10, "tx").ToList();
            Assert.Equal(2, activities.Count);
            Assert.Equal(PartialActivityType.Send, activities[0].ActivityType);
            Assert.Equal(5, activities[0].Amount);
            Assert.Equal(PartialActivityType.Receive, activities[1].ActivityType);
            Assert.Equal(10, activities[1].Amount);
            Assert.Contains("[AssetConvertSource]", activities[0].TransactionId);
            Assert.Contains("[AssetConvertTarget]", activities[1].TransactionId);
        }

        // ── CreateValuable ────────────────────────────────────────────────────

        [Fact]
        public void CreateValuable_ShouldSucceed()
        {
            var value = new Money(Currency.USD, 500);
            var activity = PartialActivity.CreateValuable(Currency.USD, TestDate, "Car", value, new Money(Currency.USD, 500), "tx");
            Assert.Equal(1, activity.Amount);
            Assert.Equal(PartialActivityType.Valuable, activity.ActivityType);
            Assert.Equal("Car", activity.Description);
            Assert.Equal(500, activity.UnitPrice!.Amount);
        }

        // ── CreateLiability ───────────────────────────────────────────────────

        [Fact]
        public void CreateLiability_ShouldSucceed()
        {
            var value = new Money(Currency.EUR, 10000);
            var activity = PartialActivity.CreateLiability(Currency.EUR, TestDate, "Mortgage", value, new Money(Currency.EUR, 10000), "tx");
            Assert.Equal(1, activity.Amount);
            Assert.Equal(PartialActivityType.Liability, activity.ActivityType);
            Assert.Equal("Mortgage", activity.Description);
            Assert.Equal(10000, activity.UnitPrice!.Amount);
        }

        // ── CreateBondRepay ───────────────────────────────────────────────────

        [Fact]
        public void CreateBondRepay_ShouldSucceed()
        {
            var unitPrice = new Money(Currency.USD, 1000);
            var activity = PartialActivity.CreateBondRepay(Currency.USD, TestDate, EmptySymbols, unitPrice, new Money(Currency.USD, 1000), "tx");
            Assert.Equal(PartialActivityType.BondRepay, activity.ActivityType);
            Assert.Equal(1000, activity.UnitPrice!.Amount);
        }

        // ── CreateCorrection ──────────────────────────────────────────────────

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCorrection_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCorrection(Currency.USD, TestDate, amount, new Money(Currency.USD, 1), "tx", "desc"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCorrection_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCorrection(Currency.USD, TestDate, 1, new Money(Currency.USD, total), "tx", "desc"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Fact]
        public void CreateCorrection_ShouldSucceed()
        {
            var activity = PartialActivity.CreateCorrection(Currency.USD, TestDate, 5, new Money(Currency.USD, 5), "tx", "Correction of a previously recorded dividend");
            Assert.Equal(5, activity.Amount);
            Assert.Equal(PartialActivityType.Correction, activity.ActivityType);
            Assert.Equal("Correction of a previously recorded dividend", activity.Description);
        }

        // ── CreateIgnore ──────────────────────────────────────────────────────

        [Fact]
        public void CreateIgnore_ShouldSucceed()
        {
            var activity = PartialActivity.CreateIgnore();
            Assert.Equal(PartialActivityType.Ignore, activity.ActivityType);
            Assert.Equal("IGNORE", activity.TransactionId);
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Fact]
        public void ToString_ShouldReturnNonEmptyString()
        {
            var activity = PartialActivity.CreateCashDeposit(Currency.USD, TestDate, 10, new Money(Currency.USD, 10), "txStr");
            var result = activity.ToString();
            Assert.NotEmpty(result);
            Assert.Contains("CashDeposit", result);
            Assert.Contains("txStr", result);
        }

        // ── Date UTC conversion ───────────────────────────────────────────────

        [Fact]
        public void Date_ShouldBeStoredAsUtc()
        {
            var localDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Local);
            var activity = PartialActivity.CreateCashDeposit(Currency.USD, localDate, 1, new Money(Currency.USD, 1), "tx");
            Assert.Equal(DateTimeKind.Utc, activity.Date.Kind);
        }
    }
}
