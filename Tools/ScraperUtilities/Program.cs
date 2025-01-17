using Microsoft.Playwright;
using ScraperUtilities.ScalableCapital;
using System;

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

			switch (arguments.Broker)
			{
				case "ScalableCapital":
					var scraper = new Scraper(page, arguments);
					await scraper.Scrape();
					break;
				default:
					throw new ArgumentException("Invalid broker entered.");
			}
		}
	}
}