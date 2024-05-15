using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Compare
{
	public class MergeActivities(IExchangeRateService exchangeRateService)
	{
		public async Task<List<MergeOrder>> Merge(IEnumerable<Holding> existingHoldings, IEnumerable<Holding> newHoldings)
		{
			var mergeOrders = new List<MergeOrder>();

			foreach (var newHolding in newHoldings)
			{
				var existingHolding = existingHoldings.FirstOrDefault(x => SymbolEquals(x, newHolding));
				if (existingHolding != null)
				{
					mergeOrders.AddRange(await Merge(existingHolding.SymbolProfile!, existingHolding.Activities, newHolding.Activities));
				}
				else
				{
					mergeOrders.AddRange(newHolding.Activities.Select(x => new MergeOrder(Operation.New, newHolding.SymbolProfile!, x)));
				}
			}

			foreach (var existingHolding in existingHoldings)
			{
				var newHolding = newHoldings.FirstOrDefault(x => x.SymbolProfile == existingHolding.SymbolProfile);
				if (newHolding == null)
				{
					mergeOrders.AddRange(existingHolding.Activities.Select(x => new MergeOrder(Operation.Removed, existingHolding.SymbolProfile!, x)));
				}
			}

			return mergeOrders;
		}

		private static bool SymbolEquals(Holding oldHolding, Holding newHolding)
		{
			if (oldHolding.SymbolProfile?.Equals(newHolding.SymbolProfile) ?? false)
			{
				return true;
			}

			if (oldHolding.SymbolProfile == null && newHolding.SymbolProfile == null)
			{
				return true;
			}

			return false;
		}

		private Task<List<MergeOrder>> Merge(SymbolProfile symbolProfile, IEnumerable<IActivity> existingActivities, IEnumerable<IActivity> newActivities)
		{
			var existingOrdersWithMatchFlag = existingActivities.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			var r = newActivities.GroupJoin(existingOrdersWithMatchFlag,
				newActivity => newActivity.TransactionId,
				existingActivity => existingActivity.Activity.TransactionId,
				(fo, eo) =>
				{
					if (eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (fo.AreEqual(exchangeRateService, other.Activity).Result)
						{
							return new MergeOrder(Operation.Duplicate, symbolProfile, fo);
						}

						return new MergeOrder(Operation.Updated, symbolProfile, other.Activity, fo);
					}

					return new MergeOrder(Operation.New, symbolProfile, fo);

				}).Union(existingOrdersWithMatchFlag
				.Where(x => !x.IsMatched)
				.Select(x => new MergeOrder(Operation.Removed, symbolProfile, x.Activity)))
				.ToList();
			return Task.FromResult(r);
		}
	}
}
