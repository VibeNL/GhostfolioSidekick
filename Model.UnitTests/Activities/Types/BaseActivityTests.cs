using Moq;
using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Activities;
using AutoFixture.Kernel;

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
			var otherActivity = new StockSplitActivity(baseActivity.Account, baseActivity.Date, 1, 2, null);

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
			baseActivity.Description = "Test";

			// Act
			var result = await baseActivity.AreEqual(exchangeRateServiceMock.Object, otherActivity);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void AllProperties_ShouldBeReadable()
		{
			// Arrang
			var type = typeof(IActivity);
			var types = AppDomain.CurrentDomain.GetAssemblies()
							.SelectMany(s => s.GetTypes())
							.Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

			Fixture fixture = new Fixture();
			foreach (var myType in types)
			{
				IActivity activity = (IActivity)fixture.Create(myType, new SpecimenContext(fixture));

				// Act & Assert
				activity.Account.Should().NotBeNull();
				activity.Date.Should().NotBe(DateTime.MinValue);
				activity.TransactionId.Should().NotBeNull();
				activity.SortingPriority.Should().NotBeNull();
				activity.Id.Should().NotBeNull();
				activity.Description.Should().NotBeNull();
			}
		}

		private record DummyActivity : BaseActivity<DummyActivity>
		{
			public DummyActivity(Account account, DateTime date)
			{
				Account = account;
				Date = date;
			}

			public override Account Account { get; }

			public override DateTime Date { get; }

			public override string? TransactionId { get; set; }

			public override int? SortingPriority { get; set; }

			public override string? Id { get; set; }

			protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, DummyActivity otherActivity)
			{
				return Task.FromResult(true);
			}
		}
	}
}
