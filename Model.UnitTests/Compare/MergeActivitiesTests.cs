using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Symbols;
using Moq;

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
		public async Task Merge_WhenNewHoldingHasNewActivities_NullProfile_ReturnsMergeOrdersWithNewOperation()
		{
			// Arrange
			var profile = fixture.Create<SymbolProfile>();
			var existingHolding = new Holding(null);
			var newHolding = new Holding(null);
			newHolding.Activities.Add(fixture.Create<BuySellActivity>());

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.New);
		}

		[Fact]
		public async Task Merge_WhenNewHoldingHasNewActivities_ReturnsMergeOrdersWithNewOperation()
		{
			// Arrange
			var profile = fixture.Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);
			newHolding.Activities.Add(fixture.Create<BuySellActivity>());

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
			existingHolding.Activities.Add(fixture.Create<BuySellActivity>());
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

			var activity1 = fixture.Build<BuySellActivity>().With(x => x.Quantity, 1).Without(x => x.Description).Create();
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

			var activity1 = fixture.Build<BuySellActivity>().With(x => x.Quantity, 1).Create();
			var activity2 = activity1 with { };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Duplicate);
		}

		[Fact]
		public async Task Merge_NewHolding_ReturnsMergeOrdersWithInsertOperation()
		{
			// Arrange
			var profile1 = new Fixture().Create<SymbolProfile>();
			var profile2 = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile1);
			var newHolding = new Holding(profile2);

			var activity1 = fixture.Create<BuySellActivity>();
			var activity2 = fixture.Create<BuySellActivity>();

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			mergeOrders.Should().BeEquivalentTo(
				[
					new GhostfolioAPI.API.MergeOrder(Operation.Removed, profile1, activity1),
					new GhostfolioAPI.API.MergeOrder(Operation.New, profile2, activity2)]
				);
		}

		[Fact]
		public async Task UnitPriceOfNull_NewActivity()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);

			var activity1 = fixture.Build<BuySellActivity>().With(x => x.Quantity, 1).Without(x => x.Description).Create();
			var activity2 = activity1 with { UnitPrice = null };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Updated);
		}

		[Fact]
		public async Task UnitPriceOfNull_OldActivity()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);

			var activity1 = fixture.Build<BuySellActivity>().With(x => x.Quantity, 1).Without(x => x.UnitPrice).Create();
			var activity2 = activity1 with { UnitPrice = new Money(Currency.EUR, 42) };
			activity1 = activity2 with { UnitPrice = null };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Updated);
		}

		[Fact]
		public async Task UnitPriceOfNull_Both()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);

			var activity1 = fixture.Build<BuySellActivity>().With(x => x.Quantity, 1).Without(x => x.UnitPrice).Create();
			var activity2 = activity1 with { Quantity = 2 };
			activity1 = activity2 with { UnitPrice = null };

			existingHolding.Activities.Add(activity1);
			newHolding.Activities.Add(activity2);

			// Act
			var mergeOrders = await new MergeActivities(exchangeRateService).Merge([existingHolding], [newHolding]);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Updated);
		}
	}
}
