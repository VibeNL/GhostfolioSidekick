using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
{
	public class ApplyDustCorrectionTests
	{
		private DateTime now = DateTime.Now;
		private DateTime next;

		public ApplyDustCorrectionTests()
		{
			next = now.AddMinutes(1);
		}

		[Fact]
		public void Execute_ShouldNotApplyDustCorrection_WhenNoProfile()
		{
			// Arrange
			var settings = new Settings { DustThreshold = 1 };
			var holding = new Holding(null!);
			var strategy = new ApplyDustCorrection(settings);

			// Act
			var result = strategy.Execute(holding);

			// Assert
			Assert.Equal(Task.CompletedTask, result);
		}

		[Fact]
		public async Task Execute_ShouldNotApplyDustCorrection_WhenOnlyBuy()
		{
			// Arrange
			var settings = new Settings { DustThreshold = 0.01m };
			var strategy = new ApplyDustCorrection(settings);
			var activity = new BuySellActivity(null!, now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					activity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			activity.Quantity.Should().Be(0.002m);
			activity.UnitPrice!.Amount.Should().Be(0.01M);
		}

		[Fact]
		public async Task Execute_ShouldNotApplyDustCorrection_WhenNoDust()
		{
			// Arrange
			var settings = new Settings { DustThreshold = 0.01m };
			var strategy = new ApplyDustCorrection(settings);
			var sellActivity = new BuySellActivity(null!, next, -0.002m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty),
					sellActivity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			sellActivity.Quantity.Should().Be(-0.002m);
			sellActivity.UnitPrice!.Amount.Should().Be(0.01M);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_WhenDustValueIsLessThanThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0.01m, DustThreshold = 0 };
			var strategy = new ApplyDustCorrection(settings);
			var sellActivity = new BuySellActivity(null!, next, -0.001m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty),
					sellActivity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			sellActivity.Quantity.Should().Be(-0.002M);
			sellActivity.UnitPrice!.Amount.Should().Be(0.005M);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_NonCrypto_WhenDustValueIsLessThanThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0, DustThreshold = 0.01m };
			var strategy = new ApplyDustCorrection(settings);
			var sellActivity = new BuySellActivity(null!, next, -0.001m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.Stock).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty),
					sellActivity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			sellActivity.Quantity.Should().Be(-0.002M);
			sellActivity.UnitPrice!.Amount.Should().Be(0.005M);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_MoreSellThanBuy_WhenDustValueIsLessThanThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0.01m, DustThreshold = 0 };
			var strategy = new ApplyDustCorrection(settings);
			var sellActivity = new BuySellActivity(null!, next, -0.002m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 0.001m, new Money(Currency.USD, 0.01m), string.Empty),
					sellActivity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			sellActivity.Quantity.Should().Be(-0.001M);
			sellActivity.UnitPrice!.Amount.Should().Be(0.02M);
		}

		[Fact]
		public async Task Execute_ShouldNotApplyDustCorrection_WhenDustValueIsEqualToThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0.01m, DustThreshold = 0 };
			var strategy = new ApplyDustCorrection(settings);
			var activity = new BuySellActivity(null!, next, -0.1m, new Money(Currency.USD, 1m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 0.2M, new Money(Currency.USD, 0.01m), string.Empty),
					activity
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			activity.Quantity.Should().Be(-0.1M);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_Bug196()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0.01m, DustThreshold = 0 };
			var strategy = new ApplyDustCorrection(settings);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 3.83656244005371m, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(1), 3.83729854182655m, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(2), 3.83729854182655m, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(3), -11.51115952000000m, new Money(Currency.USD, 0.01m), string.Empty)
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			holding.Activities.OfType<BuySellActivity>().Sum(x => x.Quantity).Should().Be(0);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_StakeRewardsAfterSell_ShouldApply()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDustThreshold = 0.01m, DustThreshold = 0 };
			var strategy = new ApplyDustCorrection(settings);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, now, 1, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(1), -1, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(2), 0.001m, new Money(Currency.USD, 0.01m), string.Empty),
					new BuySellActivity(null!, now.AddMinutes(3), 0.001m, new Money(Currency.USD, 0.01m), string.Empty)
				]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			holding.Activities.Count.Should().Be(2);
			((BuySellActivity)holding.Activities[1]).Quantity.Should().Be(-1);
			holding.Activities.OfType<BuySellActivity>().Sum(x => x.Quantity).Should().Be(0);
		}
	}
}