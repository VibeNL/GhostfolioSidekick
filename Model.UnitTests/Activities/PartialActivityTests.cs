using System;
using System.Collections.Generic;
using GhostfolioSidekick.Model.Activities;
using Xunit;

namespace GhostfolioSidekick.Model.UnitTests.Activities
{
    public class PartialActivityTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashDeposit_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashDeposit(Currency.USD, DateTime.UtcNow, amount, new Money(Currency.USD, 1), "tx1"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashDeposit_ShouldThrow_OnNegativeTotalTransactionAmount(decimal total)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashDeposit(Currency.USD, DateTime.UtcNow, 1, new Money(Currency.USD, total), "tx1"));
            Assert.Contains("TotalTransactionAmount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CreateCashDeposit_ShouldSucceed_OnNonNegativeAmount(decimal amount)
        {
            var activity = PartialActivity.CreateCashDeposit(Currency.USD, DateTime.UtcNow, amount, new Money(Currency.USD, 1), "tx1");
            Assert.Equal(amount, activity.Amount);
        }
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCashWithdrawal_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCashWithdrawal(Currency.USD, DateTime.UtcNow, amount, new Money(Currency.USD, 1), "tx2"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateBuy_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateBuy(Currency.USD, DateTime.UtcNow, new List<PartialSymbolIdentifier?>(), amount, new Money(Currency.USD, 1), new Money(Currency.USD, 1), "tx3"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSell_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSell(Currency.USD, DateTime.UtcNow, new List<PartialSymbolIdentifier?>(), amount, new Money(Currency.USD, 1), new Money(Currency.USD, 1), "tx4"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateGiftFiat_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateGift(Currency.USD, DateTime.UtcNow, amount, new Money(Currency.USD, 1), "tx5"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateGiftAsset_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateGift(DateTime.UtcNow, new List<PartialSymbolIdentifier?>(), amount, "tx6"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateSend_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateSend(DateTime.UtcNow, new List<PartialSymbolIdentifier?>(), amount, "tx7"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateReceive_ShouldThrow_OnNegativeAmount(decimal amount)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateReceive(DateTime.UtcNow, new List<PartialSymbolIdentifier?>(), amount, "tx8"));
            Assert.Contains("Amount cannot be negative", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateCurrencyConvert_ShouldThrow_OnNegativeSourceOrTarget(decimal negative)
        {
            var date = DateTime.UtcNow;
            var total = new Money(Currency.USD, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCurrencyConvert(date, new Money(Currency.USD, negative), new Money(Currency.EUR, 1), total, "tx9").GetEnumerator().MoveNext());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateCurrencyConvert(date, new Money(Currency.USD, 1), new Money(Currency.EUR, negative), total, "tx10").GetEnumerator().MoveNext());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateAssetConvert_ShouldThrow_OnNegativeSourceOrTarget(decimal negative)
        {
            var date = DateTime.UtcNow;
            var syms = new List<PartialSymbolIdentifier?>();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateAssetConvert(date, syms, negative, syms, 1, "tx11").GetEnumerator().MoveNext());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartialActivity.CreateAssetConvert(date, syms, 1, syms, negative, "tx12").GetEnumerator().MoveNext());
        }
    }
}
