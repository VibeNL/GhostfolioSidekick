using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioSync(IApiWrapper apiWrapper, ILogger<GhostfolioSync> logger) : IGhostfolioSync
	{
		private readonly IApiWrapper apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
		private readonly ILogger<GhostfolioSync> logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public async Task SyncAccount(Account account)
		{
			if (account.Platform != null)
			{
				logger.LogDebug("Syncing platform {PlatformName}", account.Platform.Name);
				var platform = await apiWrapper.GetPlatformByName(account.Platform.Name);
				if (platform == null)
				{
					logger.LogDebug("Creating platform {PlatformName}", account.Platform.Name);
					await apiWrapper.CreatePlatform(account.Platform);
					logger.LogDebug("Platform {PlatformName} created", account.Platform.Name);
				}
			}

			logger.LogDebug("Syncing account {AccountName}", account.Name);
			var ghostFolioAccount = await apiWrapper.GetAccountByName(account.Name);
			if (ghostFolioAccount == null)
			{
				logger.LogDebug("Creating account {AccountName}", account.Name);
				await apiWrapper.CreateAccount(account);
				logger.LogDebug("Account {AccountName} created", account.Name);
			}

			logger.LogDebug("Updating account {AccountName}", account.Name);
			await apiWrapper.UpdateAccount(account);
			logger.LogDebug("Account {AccountName} updated", account.Name);
		}

		public async Task SyncAllActivities(IEnumerable<Activity> allActivities)
		{
			allActivities = ConvertSendAndRecievesToBuyAndSells(allActivities);
			allActivities = ConvertGiftsToInterestOrBuy(allActivities);
			allActivities = ConvertBondRepay(allActivities);

			logger.LogDebug("Syncing activities");
			await apiWrapper.SyncAllActivities(allActivities.ToList());
			logger.LogDebug("activities synced");
		}

		public async Task SyncSymbolProfiles(IEnumerable<SymbolProfile> manualSymbolProfiles)
		{
			await apiWrapper.SyncSymbolProfiles(manualSymbolProfiles);
		}

		private static IEnumerable<Activity> ConvertSendAndRecievesToBuyAndSells(IEnumerable<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity is SendAndReceiveActivity sendAndReceiveActivity)
				{
					yield return new BuySellActivity(
						activity.Account,
						activity.Holding,
						sendAndReceiveActivity.PartialSymbolIdentifiers,
						activity.Date,
						sendAndReceiveActivity.AdjustedQuantity != 0 ? sendAndReceiveActivity.AdjustedQuantity : sendAndReceiveActivity.Quantity,
						sendAndReceiveActivity.AdjustedUnitPrice.Amount != 0 ? sendAndReceiveActivity.AdjustedUnitPrice : sendAndReceiveActivity.UnitPrice,
						activity.TransactionId,
						activity.SortingPriority,
						activity.Description)
					{
					};
				}
				else
				{
					yield return activity;
				}
			}
		}

		private static IEnumerable<Activity> ConvertGiftsToInterestOrBuy(IEnumerable<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity is GiftFiatActivity giftFiatActivity)
				{
					yield return new InterestActivity(
						giftFiatActivity.Account,
						giftFiatActivity.Holding,
						giftFiatActivity.Date,
						giftFiatActivity.Amount,
						giftFiatActivity.TransactionId,
						giftFiatActivity.SortingPriority,
						giftFiatActivity.Description)
					{
					};
				}
				else if (activity is GiftAssetActivity giftAssetActivity)
				{
					yield return new BuySellActivity(
						giftAssetActivity.Account,
						giftAssetActivity.Holding,
						giftAssetActivity.PartialSymbolIdentifiers,
						giftAssetActivity.Date,
						giftAssetActivity.AdjustedQuantity != 0 ? giftAssetActivity.AdjustedQuantity : giftAssetActivity.Quantity,
						giftAssetActivity.AdjustedUnitPrice.Amount != 0 ? giftAssetActivity.AdjustedUnitPrice : giftAssetActivity.UnitPrice,
						giftAssetActivity.TransactionId,
						giftAssetActivity.SortingPriority,
						giftAssetActivity.Description)
					{
					};
				}
				else
				{
					yield return activity;
				}
			}
		}

		private IEnumerable<Activity> ConvertBondRepay(IEnumerable<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity is RepayBondActivity repayBondActivity)
				{
					if (repayBondActivity.Holding == null)
					{
						logger.LogWarning("RepayBondActivity has no holding");
						continue;
					}

					var quantity = repayBondActivity.Holding!.Activities.OfType<BuySellActivity>().Sum(x => x.Quantity);
					var price = repayBondActivity.TotalRepayAmount.SafeDivide(quantity);

					yield return new BuySellActivity(
						activity.Account,
						activity.Holding,
						repayBondActivity.PartialSymbolIdentifiers,
						activity.Date,
						-quantity,
						price,
						activity.TransactionId,
						activity.SortingPriority,
						activity.Description)
					{
					};
				}
				else
				{
					yield return activity;
				}
			}
		}
	}
}
