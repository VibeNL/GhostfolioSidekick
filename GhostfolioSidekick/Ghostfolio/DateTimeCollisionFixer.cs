﻿using GhostfolioSidekick.Ghostfolio.API.Contract;

namespace GhostfolioSidekick.Ghostfolio
{
	public class DateTimeCollisionFixer
	{
		public static IEnumerable<Activity> Merge(IEnumerable<Activity> activities)
		{
			var mergeActivities = new[] { ActivityType.BUY, ActivityType.SELL };
			var toMerge = activities.Where(x => mergeActivities.Contains(x.Type)).ToList();

			return
				toMerge
					.GroupBy(x => Tuple.Create(x.Asset?.Symbol ?? string.Empty, x.Date.Date))
					.Select(x =>
					{
						return MergeToOne(x.ToList());

					})
					.ToList()
					.Union(activities.Where(x => !mergeActivities.Contains(x.Type)));
		}

		private static Activity MergeToOne(List<Activity> activities)
		{
			var sortedActivities = activities
				.OrderBy(x => x.Date)
				.ThenBy(x => x.ReferenceCode)
				.ThenBy(x => x.Quantity);

			var r = sortedActivities.First();

			foreach (var activity in sortedActivities.Skip(1))
			{
				r = r.Merge(activity);
			}

			r.Comment = GenerateComment(sortedActivities);

			return r;
		}

		private static string GenerateComment(IOrderedEnumerable<Activity> sortedActivities)
		{
			if (sortedActivities.Count() == 1)
			{
				return sortedActivities.Single().Comment;
			}

			return sortedActivities.First().Comment + " (" + string.Join('|', sortedActivities.Select(x => $"{x.Date.ToShortTimeString()} {x.Type} {x.Quantity}@{x.UnitPrice}")) + ")";
		}
	}
}
