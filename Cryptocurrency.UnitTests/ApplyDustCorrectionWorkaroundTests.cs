using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Cryptocurrency.UnitTests
{
	public class ApplyDustCorrectionWorkaroundTests
	{
		private readonly DateTime now = DateTime.UtcNow;
		private int c = 0;
		private readonly Fixture fixture = new();
		private readonly SymbolProfile symbolProfileCrypto;
		private readonly SymbolProfile symbolProfileStock;

		public ApplyDustCorrectionWorkaroundTests()
		{
			symbolProfileStock = fixture
				.Build<SymbolProfile>()
				.With(x => x.AssetClass, AssetClass.Equity)
				.With(x => x.AssetSubClass, AssetSubClass.Etf)
				.Create();
			symbolProfileCrypto = fixture
				.Build<SymbolProfile>()
				.With(x => x.AssetClass, AssetClass.Cash)
				.With(x => x.AssetSubClass, AssetSubClass.CryptoCurrency)
				.Create();
		}

		[Fact]
		public async Task Execute_SoldNotEverything_DustCorrected()
		{
			// Arrange
			var sg = new Settings()
			{
				CryptoWorkaroundDust = true,
				CryptoWorkaroundDustThreshold = 1,
			};
			var dust = new ApplyDustCorrectionWorkaround(sg);

			var holding = new Holding(symbolProfileCrypto)
			{
				Activities = [
					CreateDummyActivity(ActivityType.Buy, 100),
					CreateDummyActivity(ActivityType.Buy, 0.0001M),
					CreateDummyActivity(ActivityType.Sell, 100),
				]
			};

			// Act
			await dust.Execute(holding);

			// Assert
			holding.Activities.Should().HaveCount(3);
			var last = holding.Activities.Last();
			last.UnitPrice.Amount.Should().Be(0.999999000000999999000001M);
			last.Quantity.Should().Be(100.0001M);
		}

		[Fact]
		public async Task Execute_StakingReward_DustCorrected()
		{
			// Arrange
			var sg = new Settings()
			{
				CryptoWorkaroundDust = true,
				CryptoWorkaroundDustThreshold = 1,
			};
			var dust = new ApplyDustCorrectionWorkaround(sg);

			var holding = new Holding(symbolProfileCrypto)
			{
				Activities = [
					CreateDummyActivity(ActivityType.Buy, 100),
					CreateDummyActivity(ActivityType.Sell, 100),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
				]
			};

			// Act
			await dust.Execute(holding);

			// Assert
			holding.Activities.Should().HaveCount(2);
			var last = holding.Activities.Last();
			last.UnitPrice.Amount.Should().Be(0.9999970000089999730000809998M);
			last.Quantity.Should().Be(100.0003M);
		}

		[Fact]
		public async Task Execute_StakingRewardButNoSell_NotCorrected()
		{
			// Arrange
			var sg = new Settings()
			{
				CryptoWorkaroundDust = true,
				CryptoWorkaroundDustThreshold = 1,
			};
			var dust = new ApplyDustCorrectionWorkaround(sg);

			var holding = new Holding(symbolProfileCrypto)
			{
				Activities = [
					CreateDummyActivity(ActivityType.Buy, 100),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
				]
			};

			// Act
			await dust.Execute(holding);

			// Assert
			holding.Activities.Should().HaveCount(4);
		}

		[Fact]
		public async Task Execute_Disabled_DustNotCorrected()
		{
			// Arrange
			var sg = new Settings()
			{
				CryptoWorkaroundDust = false,
				CryptoWorkaroundDustThreshold = 1,
			};
			var dust = new ApplyDustCorrectionWorkaround(sg);

			var holding = new Holding(symbolProfileCrypto)
			{
				Activities = [
					CreateDummyActivity(ActivityType.Buy, 100),
					CreateDummyActivity(ActivityType.Sell, 100),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
				]
			};

			// Act
			await dust.Execute(holding);

			// Assert
			holding.Activities.Should().HaveCount(5);
		}

		[Fact]
		public async Task Execute_NoCryptoProfile_DustNotCorrected()
		{
			// Arrange
			var sg = new Settings()
			{
				CryptoWorkaroundDust = true,
				CryptoWorkaroundDustThreshold = 1,
			};
			var dust = new ApplyDustCorrectionWorkaround(sg);

			var holding = new Holding(symbolProfileStock)
			{
				Activities = [
					CreateDummyActivity(ActivityType.Buy, 100),
					CreateDummyActivity(ActivityType.Sell, 100),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
					CreateDummyActivity(ActivityType.StakingReward, 0.0001M),
				]
			};

			// Act
			await dust.Execute(holding);

			// Assert
			holding.Activities.Should().HaveCount(5);
		}

		private Activity CreateDummyActivity(ActivityType type, decimal amount)
		{
			return new Activity(null!, type, now.AddMinutes(c++), amount, new Money(Currency.EUR, 1), "A");
		}
	}
}