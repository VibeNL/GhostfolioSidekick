using Microsoft.Playwright;

namespace PortfolioViewer.WASM.UITests.PageObjects
{
	public abstract class BasePageObject
	{
		protected readonly IPage _page;

		protected BasePageObject(IPage page)
		{
			_page = page;
		}

	/// <summary>
	/// Checks if the Blazor error UI is displayed and throws an exception if found.
	/// This is automatically called after page operations to catch framework errors early.
	/// </summary>
	protected async Task CheckForBlazorErrorAsync()
	{
		try
		{
			// Only use the official Blazor error UI element selector.
			// Avoid regex/text-based selectors that trigger false positives
			// (e.g., any text containing "Reload" or "error").
			var errorElement = await _page.QuerySelectorAsync("#blazor-error-ui");
			if (errorElement != null && await errorElement.IsVisibleAsync())
			{
				var errorText = await errorElement.TextContentAsync() ?? string.Empty;
				var innerHTML = await errorElement.InnerHTMLAsync();

				Console.WriteLine($"Blazor error detected: {errorText}");
				Console.WriteLine($"Error HTML: {innerHTML}");

				// Take a screenshot for debugging
				try
				{
					var screenshotPath = $"blazor-error-{DateTime.Now:yyyyMMdd-HHmmss}.png";
					await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
					Console.WriteLine($"Screenshot saved to: {screenshotPath}");
				}
				catch { }

				throw new InvalidOperationException($"Blazor framework error occurred: {errorText.Trim()}");
			}
		}
		catch (InvalidOperationException)
		{
			throw; // Re-throw our own exception
		}
		catch
		{
			// Ignore errors while checking for errors (e.g., page closed, navigation in progress)
		}
	}

		/// <summary>
		/// Executes an action and automatically checks for Blazor errors afterwards.
		/// Use this wrapper for critical operations that should detect framework errors.
		/// </summary>
		protected async Task ExecuteWithErrorCheckAsync(Func<Task> action, CancellationToken ct = default)
		{
			await action();
			await CheckForBlazorErrorAsync();
		}

		/// <summary>
		/// Executes an action and automatically checks for Blazor errors afterwards.
		/// Returns the result of the action.
		/// </summary>
		protected async Task<T> ExecuteWithErrorCheckAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
		{
			var result = await action();
			await CheckForBlazorErrorAsync();
			return result;
		}
	}
}
