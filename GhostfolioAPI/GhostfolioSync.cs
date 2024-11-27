using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
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
						sendAndReceiveActivity.AdjustedQuantity ?? sendAndReceiveActivity.Quantity,
						sendAndReceiveActivity.AdjustedUnitPrice ?? sendAndReceiveActivity.UnitPrice,
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
				if (activity is GiftActivity giftActivity)
				{
					yield return new InterestActivity(
						activity.Account,
						activity.Holding,
						activity.Date,
						giftActivity.AdjustedUnitPrice ?? giftActivity.UnitPrice ?? new Money(Currency.EUR, 0),
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
