using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.Compare
{
	public static class MergeActivities
	{
		public static List<MergeOrder> Merge(Holding existingHolding, Holding newHolding)
		{
			var existingOrdersWithMatchFlag = existingHolding.Activities.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			return newHolding.Activities.GroupJoin(existingOrdersWithMatchFlag,
				fo => fo.TransactionId,
				eo => eo.Activity.TransactionId,
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (AreEquals(fo, other.Activity))
						{
							return new MergeOrder(Operation.Duplicate, fo);
						}

						return new MergeOrder(Operation.Updated, fo, other.Activity);
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, fo);
					}
					else
					{
						throw new NotSupportedException();
					}
				}).Union(existingOrdersWithMatchFlag
				.Where(x => !x.IsMatched)
				.Select(x => new MergeOrder(Operation.Removed, x.Activity)))
				.ToList();
		}

		private static bool AreEquals(Activity fo, Activity eo)
		{
			return
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				AreEquals(fo.Fees.ToList(), eo.Fees.ToList()) &&
				AreEquals(fo.Taxes.ToList(), eo.Taxes.ToList()) &&
				fo.ActivityType == eo.ActivityType &&
				fo.Date == eo.Date;
		}

		private static bool AreEquals(List<Money> money1, List<Money> money2)
		{
			return money1.Count == money2.Count && money1.TrueForAll(money2.Contains);
		}
	}
}
