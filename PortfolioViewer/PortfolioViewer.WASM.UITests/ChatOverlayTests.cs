using GhostfolioSidekick.Tools.TestUtilities;
using PortfolioViewer.WASM.UITests.PageObjects;

namespace PortfolioViewer.WASM.UITests;

[Collection("WebApplicationFactory")]
public class ChatOverlayTests(CustomWebApplicationFactory fixture, BrowserFixture browserFixture) : PlaywrightTestBase(fixture, browserFixture)
{
	[Fact]
	public async Task ChatOverlay_OpensAndShowsInitializationState()
	{
		Assert.True(await TestRetry.RunAsync(ChatOverlay_OpensAndShowsInitializationState_Runnable), "Test failed after all retry attempts.");
	}

	private async Task ChatOverlay_OpensAndShowsInitializationState_Runnable()
	{
		await SetupAsync();

		await ChatPageObject.OpenChatAsync(CancellationToken);

		// The WebLLM model (Qwen3-4B, multi-GB) is downloaded from HuggingFace on first open. In headless/CI environments it may still be loading or fail to initialize (no WebGPU/network), so Loading, Error and Ready are all valid renders — the point of this smoke test is that the overlay opens without a Blazor error.
		var state = await ChatPageObject.WaitForInitializationStateAsync(60_000, CancellationToken);

		Assert.True(state is ChatInitializationState.Loading or ChatInitializationState.Error or ChatInitializationState.Ready, $"Unexpected chat initialization state: {state}");
	}
}
