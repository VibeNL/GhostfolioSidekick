using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public enum ChatInitializationState
	{
		Loading,
		Error,
		Ready,
	}

	/// <summary>
	/// Page object for the floating chat overlay (rendered from MainLayout on every page).
	/// </summary>
	public class ChatPageObject(IPage page) : BasePageObject(page)
	{
		private const string ChatButtonSelector = "#chat-button";
		private const string OverlayHeaderSelector = "span:text-is(\"Sidekick Assistant\")";
		private const string LoadingPanelSelector = "text=Loading assistant";
		private const string InitializationErrorSelector = "#retry-init-button";
		private const string ChatInputSelector = "#chat-input";

		public async Task OpenChatAsync(CancellationToken ct = default)
		{
			await ExecuteWithErrorCheckAsync(async () =>
			{
				await _page.ClickAsync(ChatButtonSelector);
				await _page.WaitForSelectorAsync(OverlayHeaderSelector, new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 })
					.WaitAsync(ct);
			}, ct);
		}

		/// <summary>
		/// Waits until the overlay reaches a stable initialization state. The WebLLM model is multi-GB and downloaded from HuggingFace on first open, so in headless/CI environments it may still be loading (or fail without WebGPU/network) — all three states are valid renders.
		/// </summary>
		public async Task<ChatInitializationState> WaitForInitializationStateAsync(int timeout = 60_000, CancellationToken ct = default)
		{
			var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
			while (DateTime.UtcNow < deadline)
			{
				if (await IsVisibleAsync(InitializationErrorSelector)) return ChatInitializationState.Error;
				if (await IsVisibleAsync(ChatInputSelector)) return ChatInitializationState.Ready;
				if (await IsVisibleAsync(LoadingPanelSelector)) return ChatInitializationState.Loading;

				await Task.Delay(250, ct);
			}

			throw new TimeoutException($"Chat overlay did not reach a stable initialization state within {timeout}ms");
		}

		private async Task<bool> IsVisibleAsync(string selector)
		{
			try
			{
				var element = await _page.QuerySelectorAsync(selector);
				return element != null && await element.IsVisibleAsync();
			}
			catch
			{
				return false;
			}
		}
	}
}
