using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
	public class ScraperService(ILogger<ScraperService> logger, IPlaywright playwright) : IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Get output dir
			Console.WriteLine("Enter the output directory path:");
			var outputDirectory = Console.ReadLine();
			if (outputDirectory == null)
			{
				return;
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				Console.WriteLine("Select your scraper");
				Console.WriteLine("1. Scalable Capital");
				Console.WriteLine("2. Trade Republic");
				Console.WriteLine("3. Centraal Beheer");
				Console.WriteLine("0. Exit");
				var input = Console.ReadLine();
				if (input == null)
				{
					continue;
				}

				SupportedBrokers? broker;
				switch (input)
				{
					case "1":
						broker = SupportedBrokers.ScalableCapital;
						break;
					case "2":
						broker = SupportedBrokers.TradeRepublic;
						break;
					case "3":
						broker = SupportedBrokers.CentraalBeheer;
						break;
					case "0":
						Environment.Exit(0);
						return;
					default:
						Console.WriteLine("Invalid input.");
						continue;
				}

				await RunAsync(broker.Value, outputDirectory);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public async Task RunAsync(SupportedBrokers broker, string outputDirectory)
		{
			var browser = await playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
			var defaultContext = browser.Contexts[0];

			try
			{
				var page = defaultContext.Pages[0];

				logger.LogInformation("Starting the scraping process...");
				logger.LogInformation("Broker: {Broker}", broker);
				logger.LogInformation("Output directory: {OutputDirectory}", outputDirectory);

				IEnumerable<ActivityWithSymbol> transactions;
				switch (broker)
				{
					case SupportedBrokers.ScalableCapital:
						{
							var scraper = new ScalableCapital.Scraper(page, logger);
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					case SupportedBrokers.TradeRepublic:
						{
							var scraper = new TradeRepublic.Scraper(page, logger, outputDirectory);
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					case SupportedBrokers.CentraalBeheer:
						{
							var scraper = new CentraalBeheer.Scraper(page, logger);
							transactions = await scraper.ScrapeTransactions();
						}
						break;
					default:
						throw new ArgumentException("Invalid broker entered.");
				}

				var outputFile = Path.Combine(outputDirectory, $"{broker}.csv");
				logger.LogInformation("Output file: {OutputFile}", outputFile);

				SaveToCSV(outputFile, transactions);
				logger.LogInformation("Scraping process completed.");
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
			if (activity.Activity is BuyActivity buyActivity)
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

			if (activity.Activity is SellActivity sellActivity)
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
					Quantity = 1,
					UnitPrice = dividendActivity.Amount.Amount,
					Fee = Sum(dividendActivity.Fees.Select(x => x.Money)),
					Tax = Sum(dividendActivity.Taxes.Select(x => x.Money)),
				};
			}

			if (activity.Activity is CashDepositActivity deposit)
			{
				return new GenericRecord
				{
					ActivityType = PartialActivityType.CashDeposit,
					Symbol = activity.Symbol,
					Date = deposit.Date,
					Currency = deposit.Amount.Currency.Symbol,
					Quantity = 1,
					UnitPrice = deposit.Amount.Amount,
					Fee = 0,
					Tax = 0,
				};
			}

			if (activity.Activity is CashWithdrawalActivity withdrawal)
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
