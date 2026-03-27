using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal class InterestTransactionPage(IPage page, ILogger logger) : TransactionPage(page, logger)
	{
		protected override async Task OpenTransactionDetail(ILocator transaction)
		{
			await transaction.ScrollIntoViewIfNeededAsync();
			await Page.EvaluateAsync("window.scrollBy(0, 100)");
			await transaction.ClickAsync(new LocatorClickOptions { Position = new Position { X = 2, Y = 2 } });
			await Page.WaitForSelectorAsync("div:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
		}

		protected override async Task CloseTransactionDetail()
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
		}

		protected override async Task<Activity?> ProcessDetails(ILocator transaction)
		{
			if (await Page.GetByTestId("icon-deposit").IsVisibleAsync())
			{
				return new CashDepositActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Deposit settled"),
					TransactionId = await GetField<string>(Description),
				};
			}

			if (await Page.GetByTestId("icon-withdrawal").IsVisibleAsync())
			{
				return new CashWithdrawalActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Withdrawal settled"),
					TransactionId = await GetField<string>(Description),
				};
			}

			Logger.LogWarning("Unrecognized transaction type on interest account, skipping.");
			return null;
		}
	}
}


