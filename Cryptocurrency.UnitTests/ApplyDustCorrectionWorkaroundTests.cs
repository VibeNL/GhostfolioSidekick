using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Cryptocurrency.UnitTests
{
	public class ApplyDustCorrectionWorkaroundTests
	{
		[Fact]
		public void Execute_ShouldNotApplyDustCorrection_WhenCryptoWorkaroundDustIsFalse()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = false };
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create());
			var strategy = new ApplyDustCorrectionWorkaround(settings);

			// Act
			var result = strategy.Execute(holding);

			// Assert
			Assert.Equal(Task.CompletedTask, result);
		}

		[Fact]
		public void Execute_ShouldNotApplyDustCorrection_WhenAssetSubClassIsNotCryptoCurrency()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true };
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.PrivateEquity).Create());
			var strategy = new ApplyDustCorrectionWorkaround(settings);

			// Act
			var result = strategy.Execute(holding);

			// Assert
			Assert.Equal(Task.CompletedTask, result);
		}

		[Fact]
		public void Execute_ShouldNotApplyDustCorrection_WhenNoProfile()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true };
			var holding = new Holding(null!);
			var strategy = new ApplyDustCorrectionWorkaround(settings);

			// Act
			var result = strategy.Execute(holding);

			// Assert
			Assert.Equal(Task.CompletedTask, result);
		}

		[Fact]
		public async Task Execute_ShouldNotApplyDustCorrection_WhenOnlyBuy()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true, CryptoWorkaroundDustThreshold = 0.01m };
			var strategy = new ApplyDustCorrectionWorkaround(settings);
			var activity = new BuySellActivity(null!, DateTime.Now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty);
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
			var settings = new Settings { CryptoWorkaroundDust = true, CryptoWorkaroundDustThreshold = 0.01m };
			var strategy = new ApplyDustCorrectionWorkaround(settings);
			var activity = new BuySellActivity(null!, DateTime.Now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, DateTime.Now, -0.002m, new Money(Currency.USD, 0.01m), string.Empty),
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
		public async Task Execute_ShouldApplyDustCorrection_WhenDustValueIsLessThanThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true, CryptoWorkaroundDustThreshold = 0.01m };
			var strategy = new ApplyDustCorrectionWorkaround(settings);
			var activity = new BuySellActivity(null!, DateTime.Now, -0.001m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, DateTime.Now, 0.002m, new Money(Currency.USD, 0.01m), string.Empty),
					activity]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			activity.Quantity.Should().Be(0);
			activity.UnitPrice!.Amount.Should().Be(0.02M);
		}

		[Fact]
		public async Task Execute_ShouldApplyDustCorrection_MoreSellThanBuy_WhenDustValueIsLessThanThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true, CryptoWorkaroundDustThreshold = 0.01m };
			var strategy = new ApplyDustCorrectionWorkaround(settings);
			var activity = new BuySellActivity(null!, DateTime.Now, -0.002m, new Money(Currency.USD, 0.01m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [
					new BuySellActivity(null!, DateTime.Now, 0.001m, new Money(Currency.USD, 0.01m), string.Empty),
					activity]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			activity.Quantity.Should().Be(-0.001M);
			activity.UnitPrice!.Amount.Should().Be(0.005M);
		}

		[Fact]
		public async Task Execute_ShouldNotApplyDustCorrection_WhenDustValueIsEqualToThreshold()
		{
			// Arrange
			var settings = new Settings { CryptoWorkaroundDust = true, CryptoWorkaroundDustThreshold = 0.01m };
			var strategy = new ApplyDustCorrectionWorkaround(settings);
			var activity = new BuySellActivity(null!, DateTime.Now, -0.1m, new Money(Currency.USD, 1m), string.Empty);
			var holding = new Holding(new Fixture().Build<SymbolProfile>().With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency).Create())
			{
				Activities = [activity]
			};

			// Act
			await strategy.Execute(holding);

			// Assert
			activity.Quantity.Should().Be(-0.1M);
		}
	}
}