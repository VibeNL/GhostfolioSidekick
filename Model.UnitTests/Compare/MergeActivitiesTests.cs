using AutoFixture;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Symbols;
using Moq;
using System.Runtime.CompilerServices;

namespace GhostfolioSidekick.Model.UnitTests.Compare
{
	public class MergeActivitiesTests
	{
		private readonly Fixture fixture = new();
		private IExchangeRateService exchangeRateService;

		public MergeActivitiesTests()
		{
			var moq = new Mock<IExchangeRateService>();
			moq.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>())).ReturnsAsync(1);
			exchangeRateService = moq.Object;
		}

		[Fact]
		public async Task Merge_WhenNewHoldingHasNewActivities_ReturnsMergeOrdersWithNewOperation()
		{
			// Arrange
			var profile = fixture.Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);
			newHolding.Activities.Add(fixture.Create<Activity>());

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.New);
		}

		[Fact]
		public async Task Merge_WhenExistingHoldingHasRemovedActivities_ReturnsMergeOrdersWithRemovedOperation()
		{
			// Arrange
			var profile = fixture.Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			existingHolding.Activities.Add(fixture.Create<Activity>());
			var newHolding = new Holding(profile);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Removed);
		}

		[Fact]
		public async Task Merge_WhenExistingHoldingHasUpdatedActivities_ReturnsMergeOrdersWithUpdatedOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);

			var activity1 = fixture.Build<Activity>().With(x => x.Quantity, 1).Create();
			var activity2 = activity1 with { Quantity = 2 };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Updated);
		}

		[Fact]
		public async Task Merge_WhenExistingHoldingHasDuplicateActivities_ReturnsMergeOrdersWithDuplicateOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);

			var activity1 = fixture.Build<Activity>().With(x => x.Quantity, 1).Create();
			var activity2 = activity1 with { };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding],[newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Duplicate);
		}

	}
}
