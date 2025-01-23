using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers.Generic;
using Microsoft.Playwright;
using ScraperUtilities.ScalableCapital;
using System.Globalization;

namespace ScraperUtilities
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			var arguments = CommandLineArguments.Parse(args);
			var playWright = await Playwright.CreateAsync();
			var browser = await playWright.Chromium.LaunchAsync(
				   new BrowserTypeLaunchOptions
				   {
					   Headless = false
				   });
			var page = await browser.NewPageAsync();

			IEnumerable<ActivityWithSymbol> transactions = [];
			switch (arguments.Broker)
			{
				case "ScalableCapital":
					var scraper = new Scraper(page, arguments);
					transactions = await scraper.ScrapeTransactions();
					break;
				default:
					throw new ArgumentException("Invalid broker entered.");
			}

			SaveToCSV(arguments.OutputFile, transactions);
		}

		private static void SaveToCSV(string outputFile, IEnumerable<ActivityWithSymbol> transactions)
		{
			using (var writer = new StreamWriter(outputFile))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(transactions.Select(Transform));
			}
		}

		private static GenericRecord Transform(ActivityWithSymbol activity)
		{
			if (activity.Activity is BuySellActivity buyActivity && buyActivity.Quantity > 0)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.Buy,
					Symbol = activity.Symbol,
					Date = buyActivity.Date,
					Currency = buyActivity.UnitPrice.Currency.Symbol,
					Quantity = buyActivity.Quantity,
					UnitPrice = buyActivity.UnitPrice.Amount,
					Fee = Sum(buyActivity.Fees.Select(x => x.Money)),
					Tax = Sum(buyActivity.Taxes.Select(x => x.Money)),
				};
			}

			if (activity.Activity is BuySellActivity sellActivity && sellActivity.Quantity < 0)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.Sell,
					Symbol = activity.Symbol,
					Date = sellActivity.Date,
					Currency = sellActivity.UnitPrice.Currency.Symbol,
					Quantity = sellActivity.Quantity,
					UnitPrice = sellActivity.UnitPrice.Amount,
					Fee = Sum(sellActivity.Fees.Select(x => x.Money)),
					Tax = Sum(sellActivity.Taxes.Select(x => x.Money)),
				};
			}

			if (activity.Activity is DividendActivity dividendActivity)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.Dividend,
					Symbol = activity.Symbol,
					Date = dividendActivity.Date,
					Currency = dividendActivity.Amount.Currency.Symbol,
					Quantity = 0,
					UnitPrice = dividendActivity.Amount.Amount,
					Fee = Sum(dividendActivity.Fees.Select(x => x.Money)),
					Tax = Sum(dividendActivity.Taxes.Select(x => x.Money)),
				};
			}

			if (activity.Activity is CashDepositWithdrawalActivity deposit && deposit.Amount.Amount > 0)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.CashDeposit,
					Symbol = activity.Symbol,
					Date = deposit.Date,
					Currency = deposit.Amount.Currency.Symbol,
					Quantity = 0,
					UnitPrice = deposit.Amount.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			if (activity.Activity is CashDepositWithdrawalActivity withdrawal && withdrawal.Amount.Amount < 0)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.CashWithdrawal,
					Symbol = activity.Symbol,
					Date = withdrawal.Date,
					Currency = withdrawal.Amount.Currency.Symbol,
					Quantity = 0,
					UnitPrice = withdrawal.Amount.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			throw new ArgumentException("Invalid activity type.");
		}

		private static decimal? Sum(IEnumerable<Money> moneys)
		{
			var currency = moneys.Select(x => x.Currency.Symbol).Distinct().SingleOrDefault();
			if (currency == null)
			{
				return null;
			}

			return moneys.Sum(x => x.Amount);
		}
	}
}