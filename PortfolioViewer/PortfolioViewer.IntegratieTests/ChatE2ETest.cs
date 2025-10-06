using Microsoft.Playwright;

public class ChatE2ETest
{
	private readonly int _timeoutinMs = 60000; // Adjust timeout as needed
	private static readonly string[] options = ["--enable-unsafe-webgpu"];

	[Fact(Skip = "Work in progress")]
	public async Task Should_DisplayWebLLMResponse_When_UserSubmitsPrompt()
	{
		using var playwright = await Playwright.CreateAsync();
		var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = false,
			Args = options,
		});
		var context = await browser.NewContextAsync();
		var page = await context.NewPageAsync();

		// Navigate to your Blazor WebAssembly app
		await page.GotoAsync("http://localhost:5252");

		// Click the chat button (adjust selectors as needed)
		await page.ClickAsync("#chat-button");

		// Type a prompt
		await page.FillAsync("#chat-input", "What is my portfolio value?", new() { Timeout = _timeoutinMs });
		await page.ClickAsync("#send-button");

		// Wait for response bubble to appear
		var response = await page.WaitForSelectorAsync(".chat-response", new() { Timeout = _timeoutinMs });
		var text = response != null ? await response.InnerTextAsync() : string.Empty;

		Assert.Contains("portfolio", text); // Adjust to expected content
	}
}
