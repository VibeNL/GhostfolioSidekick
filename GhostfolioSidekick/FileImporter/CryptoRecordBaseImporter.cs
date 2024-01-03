using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CryptoRecordBaseImporter<T> : RecordBaseImporter<T>
	{
		private readonly Settings settings;

		protected CryptoRecordBaseImporter(
			ConfigurationInstance configurationInstance,
			IGhostfolioAPI api) : base(api)
		{
			settings = configurationInstance.Settings;
		}

		protected async Task<Money> GetCorrectUnitPrice(Money originalUnitPrice, SymbolProfile? symbol, DateTime date)
		{
			if (originalUnitPrice.Amount > 0)
			{
				return originalUnitPrice;
			}

			// GetPrice from Ghostfolio
			var price = await api.GetMarketPrice(symbol, date);
			return price;
		}

		protected async Task<SymbolProfile?> GetAsset(string assetName, Account account)
		{
			var mappedName = CryptoMapper.Instance.GetFullname(assetName);

			return await api.FindSymbolByIdentifier(
				new[] { mappedName, assetName },
				account.Balance.Currency,
				DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssetSubClasses);
		}

		protected override void SetActivitiesToAccount(Account account, ICollection<Activity> values)
		{
			var activities = values;

			if (settings.CryptoWorkaroundStakeReward)
			{
				// Add Staking as Dividends & Buys.
				activities = StakeWorkaround(values).ToList();
			}

			if (settings.CryptoWorkaroundDust)
			{
				// Add Dust detection
				foreach (var holding in CalculateHoldings(activities))
				{
					holding.ApplyDustCorrection();
				}
			}

			base.SetActivitiesToAccount(account, activities);
		}

		private IEnumerable<Activity> StakeWorkaround(ICollection<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity.ActivityType != ActivityType.StakingReward)
				{
					yield return activity;
					continue;
				}

				activity.ActivityType = ActivityType.Buy;
				activity.Comment += " Stake Reward";
				yield return activity;

				var div = new Activity(
					ActivityType.Dividend,
					activity.Asset,
					activity.Date,
					activity.Quantity,
					activity.UnitPrice.Times(activity.Quantity),
					[],
					activity.Comment + " workaround",
					activity.ReferenceCode + " workaround");
				yield return div;
			}
		}

		private IEnumerable<Holding> CalculateHoldings(ICollection<Activity> values)
		{
			var list = new ConcurrentDictionary<SymbolProfile, Holding>();

			foreach (var item in values.Where(x => x.Asset != null))
			{
				var holding = list.GetOrAdd(item.Asset!, new Holding());
				holding.AddActivity(item);
			}

			return list.Values;
		}
	}

	internal class Holding
	{
		public List<Activity> Activities { get; } = [];

		internal void AddActivity(Activity item)
		{
			if (item.ActivityType != ActivityType.StakingReward)
			{
				Activities.Add(item);
			}
		}

		internal void ApplyDustCorrection()
		{
			var amount = GetAmount();
			Activity lastActivity = Activities.OrderBy(x => x.Date).Last();
			var lastKnownPrice = lastActivity.UnitPrice.Amount;
			decimal dustValue = amount * lastKnownPrice;
			if (Math.Abs(dustValue) < 0.01M && dustValue != 0) // less than one cent we should correct. TODO: Make configurable
			{
				// Should always be a sell as we have dust!
				lastActivity.Quantity += amount;
			}
		}

		private decimal GetAmount()
		{
			return Activities.Sum(x => GetFactor(x) * x.Quantity);
		}

		private decimal GetFactor(Activity x)
		{
			switch (x.ActivityType)
			{
				case ActivityType.Dividend:
					return 0;
				case ActivityType.Gift:
				case ActivityType.LearningReward:
				case ActivityType.Buy:
				case ActivityType.Receive:
				case ActivityType.StakingReward:
					return 1;
				case ActivityType.Sell:
				case ActivityType.Send:
					return -1;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
