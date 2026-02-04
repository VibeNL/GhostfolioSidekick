using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public class LoginPage(IPage page) : BasePageObject(page)
	{
		private const string AccessTokenInputSelector = "input#accessToken";
		private const string SubmitButtonSelector = "button[type='submit']";
		private const string ErrorAlertSelector = ".alert-danger";

		public async Task NavigateAsync(string serverAddress)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.GotoAsync(serverAddress);
				await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			});
		}

		public async Task WaitForPageLoadAsync(int timeout = 10000)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.WaitForSelectorAsync(AccessTokenInputSelector, new PageWaitForSelectorOptions { Timeout = timeout });
			});
		}

		public async Task FillAccessTokenAsync(string token)
		{
			await _page.FillAsync(AccessTokenInputSelector, token);
		}

		public async Task ClickLoginAsync()
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.ClickAsync(SubmitButtonSelector);
			});
		}

		public async Task<bool> IsErrorDisplayedAsync()
		{
			try
			{
				var errorElement = await _page.QuerySelectorAsync(ErrorAlertSelector);
				return errorElement != null;
			}
			catch
			{
				return false;
			}
		}

		public async Task<string?> GetErrorMessageAsync()
		{
			try
			{
				var errorElement = await _page.QuerySelectorAsync(ErrorAlertSelector);
				return errorElement != null ? await errorElement.TextContentAsync() : null;
			}
			catch
			{
				return null;
			}
		}

		public async Task LoginAsync(string serverAddress, string token)
		{
			await NavigateAsync(serverAddress);
			await WaitForPageLoadAsync();
			await FillAccessTokenAsync(token);
			await ClickLoginAsync();
		}

		public async Task WaitForSuccessfulLoginAsync(int timeout = 10000)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.WaitForURLAsync(url => url.Contains("/") && !url.Contains("/login"), new PageWaitForURLOptions { Timeout = timeout });
			});
		}

		public bool IsOnLoginPage()
		{
			return _page.Url.Contains("/login");
		}
	}
}
