using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using Moq;

namespace GhostfolioSidekick.Model.UnitTests.Activities.Types
{
	public class BaseActivityTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
		private readonly DummyActivity baseActivity;

		public BaseActivityTests()
		{
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			baseActivity = new DummyActivity(new Fixture().Create<Account>(), DateTime.Now);
		}

		[Fact]
		public async Task AreEqual_SameTypeAndProperties_ReturnsTrue()
		{
			// Arrange
			var otherActivity = new DummyActivity(baseActivity.Account, baseActivity.Date);

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task AreEqual_DifferentType_ReturnsFalse()
		{
			// Arrange
			var otherActivity = new StockSplitActivity(baseActivity.Account, baseActivity.Date, 1, 2, null, null, null);

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_DifferentProperties_ReturnsFalse()
		{
			// Arrange
			var otherActivity = new DummyActivity(new Fixture().Create<Account>(), baseActivity.Date.AddDays(1));

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_DifferentPropertiesTargetNull_ReturnsFalse()
		{
			// Arrange
			var otherActivity = new DummyActivity(null!, baseActivity.Date.AddDays(1));

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_DifferentPropertiesSourceNull_ReturnsFalse()
		{
			// Arrange
			var otherActivity = new DummyActivity(null!, baseActivity.Date.AddDays(1));

			// Act
			var result = await otherActivity.AreEqual(exchangeRateServiceMock.Object, baseActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task AreEqual_DifferentDescription_ReturnsFalse()
		{
			// Arrange
			var otherActivity = new DummyActivity(baseActivity.Account, baseActivity.Date);
			baseActivity.SetDescription("Test");

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void AllProperties_ShouldBeReadable()
		{
			// Arrang
			var type = typeof(Activity);
			var types = type.Assembly.GetTypes()
							.Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

			Fixture fixture = new Fixture();
			foreach (var myType in types)
			{
				var activity = (Activity)fixture.Create(myType, new SpecimenContext(fixture));

				// Act & Assert
				activity.Account.Should().NotBeNull();
				activity.Date.Should().NotBe(DateTime.MinValue);
				activity.TransactionId.Should().NotBeNull();
				activity.SortingPriority.Should().NotBeNull();
				activity.Description.Should().NotBeNull();
			}
		}

		private record DummyActivity : Activity
		{
			public DummyActivity(Account account, DateTime date)
			{
				Account = account;
				Date = date;
			}

			protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
			{
				return Task.FromResult(true);
			}

			internal void SetDescription(string v)
			{
				Description = v;
			}
		}
	}
}
