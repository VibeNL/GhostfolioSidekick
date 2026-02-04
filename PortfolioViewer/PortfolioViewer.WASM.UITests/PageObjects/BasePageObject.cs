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
			// Try multiple selectors for different ways Blazor might display errors
			var selectors = new[]
			{
				"text=/An unhandled error has occurred.*Reload/i",
				"text=/unhandled error/i",
				"#blazor-error-ui",
				".blazor-error-boundary",
				"[style*='background:lightyellow']", // Default Blazor error UI styling
				"text=/reload/i" // The reload text is usually part of the error
			};

			foreach (var selector in selectors)
			{
				try
				{
					var errorElement = await _page.QuerySelectorAsync(selector);
					if (errorElement != null)
					{
						var isVisible = await errorElement.IsVisibleAsync();
						Console.WriteLine($"Found element with selector '{selector}', visible: {isVisible}");
						
						if (isVisible)
						{
							var errorText = await errorElement.TextContentAsync() ?? string.Empty;
							var innerHTML = await errorElement.InnerHTMLAsync();
							
							Console.WriteLine($"Blazor error detected using selector: {selector}");
							Console.WriteLine($"Error text: {errorText}");
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
				}
				catch (InvalidOperationException)
				{
					throw; // Re-throw our own exception
				}
				catch
				{
					// Continue to next selector
				}
			}
		}
		catch (InvalidOperationException)
		{
			throw; // Re-throw our own exception
		}
		catch (Exception ex)
		{
			// Ignore errors while checking for errors (e.g., page closed, navigation in progress)
			Console.WriteLine($"Could not check for Blazor error: {ex.Message}");
		}
	}

		/// <summary>
		/// Executes an action and automatically checks for Blazor errors afterwards.
		/// Use this wrapper for critical operations that should detect framework errors.
		/// </summary>
		protected async Task ExecuteWithErrorCheckAsync(Func<Task> action)
		{
			await action();
			await CheckForBlazorErrorAsync();
		}

		/// <summary>
		/// Executes an action and automatically checks for Blazor errors afterwards.
		/// Returns the result of the action.
		/// </summary>
		protected async Task<T> ExecuteWithErrorCheckAsync<T>(Func<Task<T>> action)
		{
			var result = await action();
			await CheckForBlazorErrorAsync();
			return result;
		}
	}
}
