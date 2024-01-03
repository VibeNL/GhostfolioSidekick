using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CryptoRecordBaseImporter<T> : RecordBaseImporter<T>
	{
		protected CryptoRecordBaseImporter(IGhostfolioAPI api) : base(api)
		{
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
			// Add Dust detection
			foreach (var holding in CalculateHoldings(values))
			{
				holding.ApplyDustCorrection();
			}

			base.SetActivitiesToAccount(account, values);
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
		public SortedList<DateTime, Activity> Activities { get; } = [];

		internal void AddActivity(Activity item)
		{
			this.Activities.Add(item.Date, item);
		}

		internal void ApplyDustCorrection()
		{
			var amount = GetAmount();
			Activity lastActivity = Activities.Last().Value;
			var lastKnownPrice = lastActivity.UnitPrice.Amount;
			decimal dustValue = amount * lastKnownPrice;
			if (Math.Abs(dustValue) < 0.01M && dustValue != 0) // less than one cent
			{
				lastActivity.Quantity -= amount;
			}
		}

		private decimal GetAmount()
		{
			return Activities.Values.Sum(x => GetFactor(x) * x.Quantity);
		}

		private decimal GetFactor(Activity x)
		{
			switch (x.ActivityType)
			{
				case ActivityType.Gift:
				case ActivityType.LearningReward:
				case ActivityType.StakingReward:
				case ActivityType.Buy:
				case ActivityType.Receive:
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
