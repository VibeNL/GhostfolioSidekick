using AutoFixture;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.UnitTests.Compare
{
	public class MergeActivitiesTests
	{
		[Fact]
		public void Merge_WhenNewHoldingHasNewActivities_ReturnsMergeOrdersWithNewOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			var newHolding = new Holding(profile);
			newHolding.Activities.Add(new Activity { TransactionId = "new1" });

			// Act
			var mergeOrders = MergeActivities.Merge(existingHolding, newHolding);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.New);
		}

		[Fact]
		public void Merge_WhenExistingHoldingHasRemovedActivities_ReturnsMergeOrdersWithRemovedOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			existingHolding.Activities.Add(new Activity { TransactionId = "removed1" });
			var newHolding = new Holding(profile);

			// Act
			var mergeOrders = MergeActivities.Merge(existingHolding, newHolding);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Removed);
		}

		[Fact]
		public void Merge_WhenExistingHoldingHasUpdatedActivities_ReturnsMergeOrdersWithUpdatedOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			existingHolding.Activities.Add(new Activity { TransactionId = "updated1", Quantity = 1 });
			var newHolding = new Holding(profile);
			newHolding.Activities.Add(new Activity { TransactionId = "updated1", Quantity = 2 });

			// Act
			var mergeOrders = MergeActivities.Merge(existingHolding, newHolding);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Updated);
		}

		[Fact]
		public void Merge_WhenExistingHoldingHasDuplicateActivities_ReturnsMergeOrdersWithDuplicateOperation()
		{
			// Arrange
			var profile = new Fixture().Create<SymbolProfile>();
			var existingHolding = new Holding(profile);
			existingHolding.Activities.Add(new Activity { TransactionId = "duplicate1", Quantity = 1 });
			var newHolding = new Holding(profile);
			newHolding.Activities.Add(new Activity { TransactionId = "duplicate1", Quantity = 1 });

			// Act
			var mergeOrders = MergeActivities.Merge(existingHolding, newHolding);

			// Assert
			Assert.Contains(mergeOrders, mo => mo.Operation == Operation.Duplicate);
		}

	}
}
