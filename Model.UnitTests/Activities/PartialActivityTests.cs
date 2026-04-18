using System;
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
    }
}
