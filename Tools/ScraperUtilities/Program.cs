using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers.Generic;
using Microsoft.Playwright;
using System.Globalization;

namespace ScraperUtilities
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			var arguments = new CommandLineArguments(args);
			var playWright = await Playwright.CreateAsync();
			//var browser = await playWright.Chromium.LaunchAsync(
			//	   new BrowserTypeLaunchOptions
			//	   {
			//		   Headless = false,	
			//		   //Channel = "chrome",
			//		   //ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
			//	   });
			//var context = await browser.NewContextAsync(new BrowserNewContextOptions
			//{
			//	RecordVideoDir = "C:\\Temp\\Videos",
			//	ViewportSize = new ViewportSize
			//	{
			//		Width = 1920,
			//		Height = 1080
			//	},
			//	Locale = "en-US",
			//	TimezoneId = "Europe/Amsterdam",

			//});

			var browser = await playWright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
			var defaultContext = browser.Contexts[0];

			try
			{
				//var page = await context.NewPageAsync();
				var page = defaultContext.Pages[0];

				IEnumerable<ActivityWithSymbol> transactions;
				switch (arguments.Broker)
				{
					case "ScalableCapital":
						{
							var scraper = new ScalableCapital.Scraper(page, new ScalableCapital.CommandLineArguments(args));
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					case "TradeRepublic":
						{
							var scraper = new TradeRepublic.Scraper(page, new TradeRepublic.CommandLineArguments(args));
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					default:
						throw new ArgumentException("Invalid broker entered.");
				}

				SaveToCSV(arguments.OutputFile, transactions);
			}
			finally
			{
				await defaultContext.CloseAsync();
				await browser.CloseAsync();
			}
		}

		private static void SaveToCSV(string outputFile, IEnumerable<ActivityWithSymbol> transactions)
		{
			using var writer = new StreamWriter(outputFile);
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			csv.WriteRecords(transactions.Select(Transform).OrderBy(x => x.Date));
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
					Quantity = sellActivity.Quantity * -1,
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
					Quantity = 1,
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
					Quantity = 1,
					UnitPrice = deposit.Amount.Amount * 1,
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
					Quantity = 1,
					UnitPrice = withdrawal.Amount.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			if (activity.Activity is GiftAssetActivity giftAsset)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.GiftAsset,
					Symbol = activity.Symbol,
					Date = giftAsset.Date,
					Currency = giftAsset.UnitPrice.Currency.Symbol,
					Quantity = giftAsset.Quantity,
					UnitPrice = giftAsset.UnitPrice.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			if (activity.Activity is GiftFiatActivity giftFiat)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.GiftAsset,
					Symbol = activity.Symbol,
					Date = giftFiat.Date,
					Currency = giftFiat.Amount.Currency.Symbol,
					Quantity = 1,
					UnitPrice = giftFiat.Amount.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			if (activity.Activity is InterestActivity interestActivity)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.Interest,
					Symbol = activity.Symbol,
					Date = interestActivity.Date,
					Currency = interestActivity.Amount.Currency.Symbol,
					Quantity = 1,
					UnitPrice = interestActivity.Amount.Amount,
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
