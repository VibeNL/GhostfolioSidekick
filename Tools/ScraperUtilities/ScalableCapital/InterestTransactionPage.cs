using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;

namespace GhostfolioSidekick.Tools.ScraperUtilities.ScalableCapital
{
	internal class InterestTransactionPage(IPage page, ILogger logger) : TransactionPage(page, logger)
	{
		protected override Task OpenTransactionDetail(ILocator transaction)
		{
			return Task.CompletedTask;
		}

		protected override Task CloseTransactionDetail()
		{
			return Task.CompletedTask;	
		}

		protected override async Task<Activity?> ProcessDetails(ILocator transaction)
		{
			var item = await transaction.InnerTextAsync();
			var itemHTML = await transaction.InnerHTMLAsync();

			// Date is 3 up, first child. Dateformat: Wednesday, 25 March 2026
			var dateString = await transaction.Locator("xpath=..").Locator("xpath=..").Locator("xpath=..").Locator("div").First.InnerTextAsync();
			var date = DateTime.Parse(dateString, new CultureInfo("en-US"));

			// Amount the the last child of the transaction locator. Format: €200.00
			var amountString = await transaction.Locator("div").Last.InnerTextAsync();
			var amount = decimal.Parse(amountString.Replace("€", "").Trim(), new CultureInfo("en-US"));

			if (await transaction.GetByTestId("icon-deposit").IsVisibleAsync())
			{
				return new CashDepositActivity
				{
					Amount = new Model.Money(Currency.EUR, amount),
					Date = date,
					TransactionId = (await transaction.GetAttributeAsync("data-testid")) ?? string.Empty,
				};
			}

			Logger.LogWarning("Unrecognized transaction type on interest account, skipping.");
			return null;
		}
	}
}


