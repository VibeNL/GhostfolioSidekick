using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioSync : IGhostfolioSync
	{
		private readonly IApiWrapper apiWrapper;

		public GhostfolioSync(IApplicationSettings settings, IApiWrapper apiWrapper)
		{
			ArgumentNullException.ThrowIfNull(settings);
			this.apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
		}

		public async Task SyncAccount(Account account)
		{
			if (account.Platform != null)
			{
				var platform = await apiWrapper.GetPlatformByName(account.Platform.Name);
				if (platform == null)
				{
					await apiWrapper.CreatePlatform(account.Platform);
				}
			}

			var ghostFolioAccount = await apiWrapper.GetAccountByName(account.Name);
			if (ghostFolioAccount == null)
			{
				await apiWrapper.CreateAccount(account);
			}

			await apiWrapper.UpdateAccount(account);
		}

		public async Task SyncAllActivities(IEnumerable<Activity> allActivities)
		{
			allActivities = ConvertSendAndRecievesToBuyAndSells(allActivities);
			allActivities = ConvertGiftsToInterestOrBuy(allActivities);
			allActivities = ConvertBondRepay(allActivities);

			await apiWrapper.SyncAllActivities(allActivities.ToList());
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

		private static IEnumerable<Activity> ConvertBondRepay(IEnumerable<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity is RepayBondActivity repayBondActivity)
				{
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
