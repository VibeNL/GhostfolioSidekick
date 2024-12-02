﻿using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.API.Compare
{
	public class MergeActivities()
	{
		public Task<List<MergeOrder>> Merge(IEnumerable<Activity> existingActivities, IEnumerable<Activity> newActivities)
		{
			var mergeOrders = new List<MergeOrder>();

			var newActivityByReferenceCode = newActivities.GroupBy(x => x.Comment);
			var existingActivityByReferenceCode = existingActivities.GroupBy(x => x.Comment);

			foreach (var newActivityGroup in newActivityByReferenceCode)
			{
				var existingActivityGroup = existingActivityByReferenceCode.FirstOrDefault(x => x.Key == newActivityGroup.Key);
				if (existingActivityGroup != null)
				{
					mergeOrders.AddRange(MergeMatched(existingActivityGroup, newActivityGroup));
				}
				else
				{
					mergeOrders.AddRange(newActivityGroup.Select(x => new MergeOrder(Operation.New, x.SymbolProfile, x)));
				}
			}

			foreach (var existingActivityGroup in existingActivityByReferenceCode)
			{
				if (!newActivityByReferenceCode.Any(x => x.Key == existingActivityGroup.Key))
				{
					mergeOrders.AddRange(existingActivityGroup.Select(x => new MergeOrder(Operation.Removed, x.SymbolProfile, x)));
				}
			}


			return Task.FromResult(mergeOrders);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		private IEnumerable<MergeOrder> MergeMatched(IEnumerable<Activity> existingActivityGroup, IEnumerable<Activity> newActivityGroup)
		{
			var existingListOfItems = Sortorder(existingActivityGroup.ToArray()).GroupBy(x => x.Date.Date);
			var newListOfItems = Sortorder(newActivityGroup.ToArray()).GroupBy(x => x.Date.Date);

			var compareLogic = new CompareLogic()
			{
				Config = new ComparisonConfig
				{
					MaxDifferences = int.MaxValue,
					IgnoreObjectTypes = true,
					MembersToIgnore = [nameof(Activity.Id), nameof(Activity.ReferenceCode)],
					DecimalPrecision = 5,
				}
			};

			foreach (var existingItem in existingListOfItems)
			{
				var newItem = newListOfItems.FirstOrDefault(x => x.Key == existingItem.Key);
				if (newItem != null)
				{
					// sort on date
					var existingSorted = existingItem.OrderBy(x => x.Date).ThenBy(x => x.Comment).ToList();
					var newSorted = newItem.OrderBy(x => x.Date).ThenBy(x => x.Comment).ToList();

					// compare each item individually
					for (int i = 0; i < int.Min(existingSorted.Count, newSorted.Count); i++)
					{
						var comparisonResult = compareLogic.Compare(existingSorted[i], newSorted[i]);
						if (!comparisonResult.AreEqual)
						{
							yield return new MergeOrder(Operation.Updated, existingSorted[i].SymbolProfile, existingSorted[i], newSorted[i]);
						}
					}

					// if the new list is longer, then we have new items
					if (newSorted.Count > existingSorted.Count)
					{
						for (int i = existingSorted.Count; i < newSorted.Count; i++)
						{
							yield return new MergeOrder(Operation.New, newSorted[i].SymbolProfile, newSorted[i]);
						}
					}

					// if the existing list is longer, then we have removed items
					if (existingSorted.Count > newSorted.Count)
					{
						for (int i = newSorted.Count; i < existingSorted.Count; i++)
						{
							yield return new MergeOrder(Operation.Removed, existingSorted[i].SymbolProfile, existingSorted[i]);
						}
					}
				}
				else
				{
					foreach (var activity in existingItem)
					{
						yield return new MergeOrder(Operation.Removed, activity.SymbolProfile, activity);
					}
				}
			}

			foreach (var listbItem in newListOfItems)
			{
				var listaItem = existingListOfItems.FirstOrDefault(x => x.Key == listbItem.Key);
				if (listaItem == null)
				{
					foreach (var activity in listbItem)
					{
						yield return new MergeOrder(Operation.New, activity.SymbolProfile, activity);
					}
				}
			}


			static List<Activity> Sortorder(Activity[] existingActivities)
			{
				return existingActivities
						.OrderBy(x => x.Date)
						.ThenBy(x => x.SymbolProfile.Symbol)
						.ThenBy(x => x.Comment)
						.ToList();
			}
		}
	}
}
