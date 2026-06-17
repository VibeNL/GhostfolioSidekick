using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
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
				Platform? platform = await apiWrapper.GetPlatformByName(account.Platform.Name);
				if (platform == null)
				{
					logger.LogDebug("Creating platform {PlatformName}", account.Platform.Name);
					await apiWrapper.CreatePlatform(account.Platform);
					logger.LogDebug("Platform {PlatformName} created", account.Platform.Name);
				}
			}

			logger.LogDebug("Syncing account {AccountName}", account.Name);
			Account? ghostFolioAccount = await apiWrapper.GetAccountByName(account.Name);
			if (ghostFolioAccount == null)
			{
				logger.LogDebug("Creating account {AccountName}", account.Name);
				await apiWrapper.CreateAccount(account);
				logger.LogDebug("Account {AccountName} created", account.Name);
			}

			// Only update account (which includes balance sync) if SyncBalance is enabled
			if (account.SyncBalance)
			{
				logger.LogDebug("Updating account {AccountName}", account.Name);
				await apiWrapper.UpdateAccount(account);
				logger.LogDebug("Account {AccountName} updated", account.Name);
			}
			else
			{
				logger.LogDebug("Skipping account balance update for {AccountName} (SyncBalance is disabled)", account.Name);
			}
		}

		public async Task SyncAllActivities(IEnumerable<Activity> allActivities)
		{
			logger.LogDebug("Syncing activities");

			allActivities = ConvertSendAndRecievesToBuyAndSells(allActivities);
			allActivities = ConvertGiftsToInterestOrBuy(allActivities);
			allActivities = ConvertBondRepay(allActivities);
			allActivities = SettleNegativeDividends(allActivities);
			List<Activity> allactivitiesList = allActivities.Where(x => x.Account.SyncActivities).ToList();

			await apiWrapper.SyncAllActivities([.. allactivitiesList]);
			logger.LogDebug("activities synced");
		}

		public async Task SyncSymbolProfiles(IEnumerable<SymbolProfile> manualSymbolProfiles)
		{
			await apiWrapper.SyncSymbolProfiles(manualSymbolProfiles);
		}

		public async Task SyncMarketData(SymbolProfile profile, ICollection<MarketData> list)
		{
			await apiWrapper.SyncMarketData(profile, list);
		}

		private static IEnumerable<Activity> ConvertSendAndRecievesToBuyAndSells(IEnumerable<Activity> activities)
		{
			foreach (Activity activity in activities)
			{
				if (activity is ReceiveActivity receiveActivity)
				{
					yield return ConvertReceiveToBuy(receiveActivity);
				}
				else if (activity is SendActivity sendActivity)
				{
					yield return ConvertSendToSell(sendActivity);
				}
				else
				{
					yield return activity;
				}
			}
		}

		private static BuyActivity ConvertReceiveToBuy(ReceiveActivity receiveActivity)
		{
			return new BuyActivity(
				receiveActivity.Account,
				receiveActivity.Holding,
				receiveActivity.PartialSymbolIdentifiers,
				receiveActivity.Date,
				receiveActivity.AdjustedQuantity != 0 ? receiveActivity.AdjustedQuantity : receiveActivity.Quantity,
				receiveActivity.AdjustedUnitPrice.Amount != 0 ? receiveActivity.AdjustedUnitPrice : receiveActivity.UnitPrice,
				receiveActivity.UnitPrice.Times(receiveActivity.Quantity),
				receiveActivity.TransactionId,
				receiveActivity.SortingPriority,
				receiveActivity.Description)
			{
			};
		}

		private static SellActivity ConvertSendToSell(SendActivity sendActivity)
		{
			return new SellActivity(
				sendActivity.Account,
				sendActivity.Holding,
				sendActivity.PartialSymbolIdentifiers,
				sendActivity.Date,
				sendActivity.AdjustedQuantity != 0 ? sendActivity.AdjustedQuantity : sendActivity.Quantity,
				sendActivity.AdjustedUnitPrice.Amount != 0 ? sendActivity.AdjustedUnitPrice : sendActivity.UnitPrice,
				sendActivity.UnitPrice.Times(sendActivity.Quantity),
				sendActivity.TransactionId,
				sendActivity.SortingPriority,
				sendActivity.Description)
			{
			};
		}

		private static IEnumerable<Activity> ConvertGiftsToInterestOrBuy(IEnumerable<Activity> activities)
		{
			foreach (Activity activity in activities)
			{
				Activity result;
				if (activity is GiftFiatActivity giftFiatActivity)
				{
					result = new InterestActivity(
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
				else
				{
					if (activity is GiftAssetActivity giftAssetActivity)
					{
						decimal quantity = giftAssetActivity.AdjustedQuantity != 0 ? giftAssetActivity.AdjustedQuantity : giftAssetActivity.Quantity;
						Money unitPrice = giftAssetActivity.AdjustedUnitPrice.Amount != 0 ? giftAssetActivity.AdjustedUnitPrice : giftAssetActivity.UnitPrice;
						result = new BuyActivity(
							giftAssetActivity.Account,
							giftAssetActivity.Holding,
							giftAssetActivity.PartialSymbolIdentifiers,
							giftAssetActivity.Date,
							quantity,
							unitPrice,
							giftAssetActivity.UnitPrice.Times(giftAssetActivity.Quantity),
							giftAssetActivity.TransactionId,
							giftAssetActivity.SortingPriority,
							giftAssetActivity.Description);
					}
					else
					{
						result = activity;
					}
				}

				yield return result;
			}
		}

		private IEnumerable<Activity> ConvertBondRepay(IEnumerable<Activity> activities)
		{
			foreach (Activity activity in activities)
			{
				if (activity is RepayBondActivity repayBondActivity)
				{
					if (repayBondActivity.Holding == null)
					{
						logger.LogWarning("RepayBondActivity has no holding");
						continue;
					}

					decimal buyQuantity = repayBondActivity.Holding!.Activities.OfType<BuyActivity>().Sum(x => x.Quantity);
					decimal sellQuantity = repayBondActivity.Holding!.Activities.OfType<SellActivity>().Sum(x => x.Quantity);
					decimal quantity = buyQuantity - sellQuantity;

					Money price = repayBondActivity.Amount.SafeDivide(quantity);

					yield return new SellActivity(
						activity.Account,
						activity.Holding,
						repayBondActivity.PartialSymbolIdentifiers,
						activity.Date,
						quantity,
						price,
						price.Times(quantity),
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

		private static IEnumerable<Activity> SettleNegativeDividends(IEnumerable<Activity> activities)
		{
			foreach (Activity activity in activities)
			{
				if (activity is DividendActivity divided && divided.Amount.Amount < 0)
				{
					// If the dividend is negative, we assume it is a correction of aprevious dividend.
					// See for now mark it as a fee activity.
					yield return new FeeActivity(
						activity.Account,
						activity.Holding,
						activity.Date,
						divided.Amount.Times(-1),
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
