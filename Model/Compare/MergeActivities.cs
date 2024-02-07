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

		private Task<List<MergeOrder>> Merge(SymbolProfile symbolProfile, IEnumerable<Activity> existingActivities, IEnumerable<Activity> newActivities)
		{
			var existingOrdersWithMatchFlag = existingActivities.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			var r = newActivities.GroupJoin(existingOrdersWithMatchFlag,
				newActivity => newActivity.TransactionId,
				existingActivity => existingActivity.Activity.TransactionId,
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

						return new MergeOrder(Operation.Updated, symbolProfile, other.Activity, fo);
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

		private async Task<bool> AreEquals(Activity newActivity, Activity existingActivity)
		{
			var existingUnitPrice = await RoundAndConvert(existingActivity.UnitPrice, newActivity.UnitPrice.Currency, newActivity.Date);
			var quantityTimesUnitPriceEquals = Math.Abs(
				(newActivity.Quantity * newActivity.UnitPrice.Amount) -
				existingActivity.Quantity * existingUnitPrice.Amount) < 0.00001M;
			var feesAndTaxesEquals = AreEquals(
				existingActivity.UnitPrice.Currency,
				existingActivity.Date,
				newActivity.Fees.Union(newActivity.Taxes).ToList(),
				existingActivity.Fees.Union(existingActivity.Taxes).ToList());
			var activityEquals = newActivity.ActivityType == existingActivity.ActivityType;
			var dateEquals = newActivity.Date == existingActivity.Date;
			var descriptionEquals = newActivity.Description == null || newActivity.Description == existingActivity.Description; // We do not create descrptions when Ghostfolio will ignore them
			var equals = quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals &&
				activityEquals &&
				dateEquals &&
				descriptionEquals;
			return equals;
		}

		private bool AreEquals(Currency target, DateTime dateTime, List<Money> money1, List<Money> money2)
		{
			return money1.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			}) == money2.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			});
		}

		private async Task<Money> RoundAndConvert(Money value, Currency target, DateTime dateTime)
		{
			static decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			}

			var rate = await exchangeRateService.GetConversionRate(value.Currency, target, dateTime);
			return new Money(target, Round(value.Amount * rate));
		}
	}
}
