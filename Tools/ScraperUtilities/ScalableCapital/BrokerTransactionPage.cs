using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal class BrokerTransactionPage(IPage page, ILogger logger) : TransactionPage(page, logger)
	{
		protected override async Task OpenTransactionDetail(ILocator transaction)
		{
			await transaction.ScrollIntoViewIfNeededAsync();
			await Page.EvaluateAsync("window.scrollBy(0, 100)");
			await transaction.ClickAsync(new LocatorClickOptions { Position = new Position { X = 2, Y = 2 } }); // avoid clicking any links
			await Page.WaitForSelectorAsync("div:text('Overview')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
		}

		protected override async Task CloseTransactionDetail()
		{
			await Page.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
		}

		protected override async Task SetupFilters()
		{
			Logger.LogInformation("Setting status to Executed only...");
			await Page.GetByRole(AriaRole.Button).GetByText("Status").ClickAsync();
			await Page.GetByTestId("EXECUTED").Locator("div").First.ClickAsync();

			Thread.Sleep(1000);
			await Page.Mouse.ClickAsync(2, 2);
			Logger.LogInformation("Status set to Executed only.");
		}

		protected override async Task<Activity?> ProcessDetails(ILocator transaction)
		{
			// If is Interest
			if (await Page.GetByTestId("icon-INTEREST").IsVisibleAsync())
			{
				var dateInterest = await GetHistoryDate("Interest booked\r\n");

				return new InterestActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateInterest,
					TransactionId = await GetField<string>(Description),
				};
			}

			var otherDiv = Page.GetByTestId("icon-OTHERS");
			if (await otherDiv.IsVisibleAsync())
			{
				var headerText = await otherDiv.Locator("..").InnerTextAsync();
				if (headerText.Contains("Withdrawal"))
				{
					var dateDeposit = await GetHistoryDate("Payment settled");

					return new CashWithdrawalActivity
					{
						Amount = (await GetMoneyField("Amount")).Times(-1),
						Date = dateDeposit,
						TransactionId = await GetField<string>(Description),
					};
				}
			}

			// If is Deposit or Withdrawal
			if (await Page.GetByTestId("icon-DEPOSIT").IsVisibleAsync())
			{
				var dateDeposit = await GetHistoryDate("Deposit settled");

				return new CashDepositActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = dateDeposit,
					TransactionId = await GetField<string>(Description),
				};
			}

			if (await Page.GetByTestId("icon-WITHDRAWAL").IsVisibleAsync())
			{
				var dateWithdrawal = await GetHistoryDate("Withdrawal settled");

				return new CashWithdrawalActivity
				{
					Amount = (await GetMoneyField("Amount")).Times(-1),
					Date = dateWithdrawal,
					TransactionId = await GetField<string>(Description),
				};
			}

			// If is Buy or Sell
			var isSaving = await Page.GetByTestId("icon-SAVINGS_PLAN").IsVisibleAsync();
			var isBuy = await Page.GetByTestId("icon-BUY").IsVisibleAsync();
			var isSell = await Page.GetByTestId("icon-SELL").IsVisibleAsync();
			var isReinvest = await Page.GetByTestId("icon-REINVESTMENT").IsVisibleAsync();

			var isSecurity = await Page.GetByTestId("icon-SECURITY").IsVisibleAsync();
			if (isSecurity)
			{
				// Get parent div & compare text to 'Buy' or 'Sell'
				var icon = Page.GetByTestId("icon-SECURITY");
				var parent = icon.Locator("..");
				var text = await parent.InnerTextAsync();

				switch (text)
				{
					case string s when s.Contains("Buy"):
						isBuy = true;
						break;
					case string s when s.Contains("Sell"):
						isSell = true;
						break;
					default:
						throw new NotSupportedException();
				}
			}

			if (isSaving ||
				isBuy ||
				isSell ||
				isReinvest)
			{
				var date = await GetHistoryDate("Execution confirmed");
				Money? fee = null;
				if (!isSaving && !isReinvest)
				{
					fee = await GetMoneyField("Order fee");
				}

				if (isSell)
				{
					return new SellActivity
					{
						Quantity = await GetField<decimal>("Executed quantity"),
						UnitPrice = await GetMoneyField("Execution price"),
						TotalTransactionAmount = await GetMoneyField("Market valuation"),
						Fees = fee != null ? [new SellActivityFee(fee)] : [],
						Date = date,
						TransactionId = await GetField<string>(Description),
					};
				}

				return new BuyActivity
				{
					Quantity = await GetField<decimal>("Executed quantity"),
					UnitPrice = await GetMoneyField("Execution price"),
					TotalTransactionAmount = await GetMoneyField("Market valuation"),
					Fees = fee != null ? [new BuyActivityFee(fee)] : [],
					Date = date,
					TransactionId = await GetField<string>(Description),
				};
			}

			// If is Distribution
			if (await Page.GetByTestId("icon-DIVIDEND").IsVisibleAsync())
			{
				return new DividendActivity
				{
					Amount = await GetMoneyField("Amount"),
					Date = await GetHistoryDate("Dividend settled"),
					TransactionId = await GetField<string>(Description),
				};
			}

			// If is Transfer In or Out
			if (await Page.GetByTestId("icon-TRANSFER_IN").IsVisibleAsync())
			{
				Logger.LogWarning("Ignoring TRANSFER IN transaction.");
				return null;
			}

			if (await Page.GetByTestId("icon-TRANSFER_OUT").IsVisibleAsync())
			{
				Logger.LogWarning("Ignoring TRANSFER OUT transaction.");
				return null;
			}

			throw new NotSupportedException();
		}
	}
}
