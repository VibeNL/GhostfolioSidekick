using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using FluentAssertions;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class ActivityManagerTests
    {
        private readonly List<Account> _accounts;
        private readonly ActivityManager _activityManager;

        public ActivityManagerTests()
        {
            _accounts = new List<Account>
            {
                new Account { Name = "Account1", Id = 1 },
                new Account { Name = "Account2", Id = 2 }
            };
            _activityManager = new ActivityManager(_accounts);
        }

        [Fact]
        public async Task AddPartialActivity_ShouldAddPartialActivities()
        {
            // Arrange
            var partialActivities = new List<PartialActivity>
            {
                new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1")
            };

            // Act
            _activityManager.AddPartialActivity("Account1", partialActivities);

            // Assert
            var activities =await  _activityManager.GenerateActivities();
            activities.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateActivities_ShouldGenerateActivities()
        {
            // Arrange
            var partialActivities = new List<PartialActivity>
            {
                new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1")
            };
            _activityManager.AddPartialActivity("Account1", partialActivities);

            // Act
            var activities = await _activityManager.GenerateActivities();

            // Assert
            activities.Should().HaveCount(1);
            activities.First().Should().BeOfType<BuySellActivity>();
        }

        [Fact]
        public async Task GenerateActivities_ShouldHandleMultiplePartialActivities()
        {
            // Arrange
            var partialActivities = new List<PartialActivity>
            {
                new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1"),
                new PartialActivity(PartialActivityType.Fee, DateTime.Now, Currency.USD, new Money(Currency.USD, 10), "T1")
            };
            _activityManager.AddPartialActivity("Account1", partialActivities);

            // Act
            var activities = await _activityManager.GenerateActivities();

            // Assert
            activities.Should().HaveCount(1);
            var activity = activities.First() as BuySellActivity;
            activity.Should().NotBeNull();
            activity!.Fees.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateActivities_ShouldClearUnusedPartialActivities()
        {
            // Arrange
            var partialActivities = new List<PartialActivity>
            {
                new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "T1")
            };
            _activityManager.AddPartialActivity("Account1", partialActivities);

            // Act
            await _activityManager.GenerateActivities();

            // Assert
            var activities = await _activityManager.GenerateActivities();
            activities.Should().BeEmpty();
        }
    }
}
