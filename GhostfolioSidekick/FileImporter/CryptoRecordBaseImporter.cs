using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;

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
			return price ?? new Money(symbol.Currency, 0, date);
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
				activities = CryptoWorkarounds.StakeWorkaround(activities).ToList();
			}

			if (settings.CryptoWorkaroundDust)
			{
				// Add Dust detection
				activities = CryptoWorkarounds.DustWorkaround(activities, settings.CryptoWorkaroundDustThreshold).ToList();
			}

			base.SetActivitiesToAccount(account, activities);
		}
	}
}
