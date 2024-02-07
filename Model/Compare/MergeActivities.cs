using GhostfolioSidekick.GhostfolioAPI.API;
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
				var existingHolding = existingHoldings.FirstOrDefault(x => x.SymbolProfile == newHolding.SymbolProfile);
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

		private Task<List<MergeOrder>> Merge(SymbolProfile symbolProfile, IEnumerable<Activity> existingActivities, IEnumerable<Activity> newActivities)
		{
			var existingOrdersWithMatchFlag = existingActivities.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			var r = newActivities.GroupJoin(existingOrdersWithMatchFlag,
				fo => fo.TransactionId,
				eo => eo.Activity.TransactionId,
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (AreEquals(fo, other.Activity).Result)
						{
							return new MergeOrder(Operation.Duplicate, symbolProfile, fo);
						}

						return new MergeOrder(Operation.Updated, symbolProfile, fo, other.Activity);
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, symbolProfile, fo);
					}
					else
					{
						throw new NotSupportedException();
					}
				}).Union(existingOrdersWithMatchFlag
				.Where(x => !x.IsMatched)
				.Select(x => new MergeOrder(Operation.Removed, symbolProfile, x.Activity)))
				.ToList();
			return Task.FromResult(r);
		}

		private async Task<bool> AreEquals(Activity fo, Activity eo)
		{
			var quantityEquals = fo.Quantity == eo.Quantity;
			var unitPriceEquals = fo.UnitPrice.Equals(await RoundAndConvert(eo.UnitPrice, fo.UnitPrice.Currency, fo.Date));
			var feesEquals = AreEquals(fo.Fees.ToList(), eo.Fees.ToList());
			var taxesEquals = AreEquals(fo.Taxes.ToList(), eo.Taxes.ToList());
			var activityEquals = fo.ActivityType == eo.ActivityType;
			var dateEquals = fo.Date == eo.Date;
			return
				quantityEquals &&
				unitPriceEquals &&
				feesEquals &&
				taxesEquals &&
				activityEquals &&
				dateEquals;
		}

		private static bool AreEquals(List<Money> money1, List<Money> money2)
		{
			return money1.Count == money2.Count && money1.TrueForAll(money2.Contains);
		}

		private async Task<Money> RoundAndConvert(Money value, Currency target, DateTime dateTime)
		{
			static decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			}

			var rate = await exchangeRateService.GetConversionRate(value.Currency, target, dateTime);
			return new Money(value.Currency, Round(value.Amount * rate));
		}
	}
}
