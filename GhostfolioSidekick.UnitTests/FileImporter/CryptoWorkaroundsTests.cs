using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter
{
	public class CryptoWorkaroundsTests
	{
		readonly Mock<IGhostfolioAPI> api;
		private readonly Mock<IApplicationSettings> cs;

		public CryptoWorkaroundsTests()
		{
			api = new Mock<IGhostfolioAPI>();
			cs = new Mock<IApplicationSettings>();

		}

		[Fact]
		public void StakeWorkaround_NoStakeReward_Unchanged()
		{
			// Arrange
			var activities = new Fixture()
								.Build<Activity>()
								.With(x => x.ActivityType, ActivityType.Buy)
								.CreateMany(1)
								.ToList();

			// Act
			var updatedActivities = CryptoWorkarounds.StakeWorkaround(activities).ToList();

			// Assert
			updatedActivities.Should().HaveCount(1);
		}

		[Fact]
		public void StakeWorkaround_SingleStakeReward_Converted()
		{
			// Arrange
			var activities = new Fixture()
								.Build<Activity>()
								.With(x => x.ActivityType, ActivityType.StakingReward)
								.CreateMany(1)
								.ToList();

			// Act
			var updatedActivities = CryptoWorkarounds.StakeWorkaround(activities).ToList();

			// Assert
			updatedActivities.Should().HaveCount(2);
			var buy = updatedActivities.Single(x => x.ActivityType == ActivityType.Buy);
			var dividend = updatedActivities.Single(x => x.ActivityType == ActivityType.Dividend);

			var buyValue = buy.UnitPrice.Times(buy.Quantity);
			var divValue = dividend.UnitPrice.Times(dividend.Quantity);
			buyValue.Amount.Should().Be(divValue.Amount);
		}

		[Fact]
		public void DustWorkaround_NoDust_Unchanged()
		{
			// Arrange
			var quantity = 42;
			var asset = new Fixture().Create<SymbolProfile>();
			var activities = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.Buy)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 100, DateTime.Now))
								.With(x => x.Quantity, quantity)
								.CreateMany(1)
								.ToList();

			// Act
			var resultingActivities = CryptoWorkarounds.DustWorkaround(activities, 1M);

			// Assert
			resultingActivities.Should().HaveCount(1);
			activities.Single().Quantity.Should().Be(quantity);
		}

		[Fact]
		public void DustWorkaround_Dust_LastActivityUpdated()
		{
			// Arrange
			var quantity = 100;
			var asset = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.AssetSubClass, AssetSubClass.CRYPTOCURRENCY)
				.Create();
			var buy = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.Buy)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 0.1M, DateTime.Now))
								.With(x => x.Quantity, quantity)
								.With(x => x.Date, DateTime.Today.AddDays(-2))
								.Create();
			var sell = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.Sell)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 0.1M, DateTime.Now))
								.With(x => x.Quantity, quantity - 1)
								.With(x => x.Date, DateTime.Today.AddDays(-1))
								.Create();
			IEnumerable<Activity> activities = [buy, sell];

			// Act
			var resultingActivities = CryptoWorkarounds.DustWorkaround(activities, 1M);

			// Assert
			resultingActivities.Should().HaveCount(2);
			sell.Quantity.Should().Be(quantity);
			sell.UnitPrice.Amount.Should().Be(0.099M);
		}

		[Fact]
		public void DustWorkaround_DustAndStakingRewards_LastActivityUpdatedAndStakingRewardsRemoved()
		{
			// Arrange
			var quantity = 100;
			var asset = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.AssetSubClass, AssetSubClass.CRYPTOCURRENCY)
				.Create();
			var buy = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.Buy)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 0.1M, DateTime.Now))
								.With(x => x.Quantity, quantity)
								.With(x => x.Date, DateTime.Today.AddDays(-3))
								.Create();
			var sell = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.Sell)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 0.1M, DateTime.Now))
								.With(x => x.Quantity, quantity - 1)
								.With(x => x.Date, DateTime.Today.AddDays(-2))
								.Create();
			var stakeReward = new Fixture()
								.Build<Activity>()
								.With(x => x.Asset, asset)
								.With(x => x.ActivityType, ActivityType.StakingReward)
								.With(x => x.UnitPrice, new Money(DefaultCurrency.USD, 0.1M, DateTime.Now))
								.With(x => x.Quantity, 0.0000001M)
								.With(x => x.Date, DateTime.Today.AddDays(-1))
								.Create();
			IEnumerable<Activity> activities = [buy, sell, stakeReward];

			// Act
			var resultingActivities = CryptoWorkarounds.DustWorkaround(activities, 1M);

			// Assert
			resultingActivities.Should().HaveCount(2);
			sell.Quantity.Should().Be(quantity + 0.0000001M);
			sell.UnitPrice.Amount.Should().Be(0.0989999999010000000989999999M);
		}
	}
}
