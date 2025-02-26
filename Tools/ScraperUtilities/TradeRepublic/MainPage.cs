using GhostfolioSidekick.Model.Symbols;
using Microsoft.Playwright;

namespace ScraperUtilities.TradeRepublic
{
	public class MainPage
	{
		private IPage page;

		public MainPage(IPage page)
		{
			this.page = page;
		}
		
		internal async Task<TransactionPage> GoToTransactions()
		{
			await page.GotoAsync("https://app.traderepublic.com/profile/transactions");

			// Wait for transactions to load
            await page.WaitForSelectorAsync("span:text('Withdraw')", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

			return new TransactionPage(page);
		}

		internal async Task<IReadOnlyCollection<ILocator>> GetPortfolios()
		{
			var brokercards = await page.GetByTestId("broker-card").AllAsync();
			return brokercards;
		}

		internal async Task SwitchToAccount(ILocator account)
		{
			await account.ClickAsync();
		}

		internal async Task<ICollection<SymbolProfile>> ScrapeSymbols()
		{
			await page.GotoAsync("https://app.traderepublic.com/portfolio");
			var returnList = new List<SymbolProfile>();

			while (returnList.Count == 0)
			{
				var symbolList = page.Locator("ul[class='portfolioInstrumentList']").First;

				var symbols = await symbolList.Locator("li").AllAsync();


				foreach (var symbol in symbols)
				{
					var isin = await symbol.GetAttributeAsync("id");
					var name = await symbol.Locator("span[class='instrumentListItem__name']").TextContentAsync();
					returnList.Add(new SymbolProfile { ISIN = isin, Name = name });
				}
			}

			return returnList;
		}
	}
}