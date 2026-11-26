using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public class LoginPage(IPage page) : BasePageObject(page)
	{
		private const string AccessTokenInputSelector = "input#accessToken";
		private const string SubmitButtonSelector = "button[type='submit']";
		private const string ErrorAlertSelector = ".alert-danger";

		// Mirrors HomePage's dashboard indicator — used to detect the already-authenticated state.
		private const string DashboardHeadingSelector = "h1:has-text('Dashboard')";

		public async Task NavigateAsync(string serverAddress, CancellationToken ct = default)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.GotoAsync(serverAddress);
				await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			}, ct);
		}

		public async Task<string> CapturePageContentAsync()
		{
			return await _page.ContentAsync();
		}

		public async Task FillAccessTokenAsync(string token)
		{
			await _page.FillAsync(AccessTokenInputSelector, token);
		}

		public async Task ClickLoginAsync(CancellationToken ct = default)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.ClickAsync(SubmitButtonSelector);
			}, ct);
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

		public async Task LoginAsync(string serverAddress, string token, CancellationToken ct = default)
		{
			await NavigateAsync(serverAddress, ct);

			// On TestRetry attempts the context already holds a valid token in localStorage, so the app auto-authenticates
			// and renders the dashboard instead of the login form. Skip re-login in that case — waiting for the (absent)
			// input would time out and mask the real failure from attempt 1.
			if (!await WaitForLoginOrDashboardAsync(ct))
			{
				return;
			}

			await FillAccessTokenAsync(token);
			await ClickLoginAsync(ct);
		}

		private async Task<bool> WaitForLoginOrDashboardAsync(CancellationToken ct)
		{
			// Comma-separated selector list = "any of" in CSS.
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.WaitForSelectorAsync($"{AccessTokenInputSelector}, {DashboardHeadingSelector}", new PageWaitForSelectorOptions { Timeout = 15_000 });
			}, ct);

			return await _page.QuerySelectorAsync(AccessTokenInputSelector) != null;
		}

		public async Task WaitForSuccessfulLoginAsync(int timeout = 10000)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.WaitForURLAsync(url => url.Contains('/') && !url.Contains("/login"), new PageWaitForURLOptions { Timeout = timeout });
			});
		}

		public bool IsOnLoginPage()
		{
			return _page.Url.Contains("/login");
		}
	}
}
