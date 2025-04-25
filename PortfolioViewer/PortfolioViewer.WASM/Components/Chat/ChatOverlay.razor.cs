using System.Data;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Chat
{
	public partial class ChatOverlay
	{
		private bool IsOpen = false;
		private string CurrentMessage = "";
		private bool IsBotTyping = false;
		private bool IsInitialized = false; // Flag to track initialization

		private List<ChatMessage> Messages = [];

		private IWebChatClient chatClient;

		private Progress<InitializeProgress> progress = new();
		private string streamingText = "";
		private InitializeProgress lastProgress = new(0);

		private IJSRuntime JS { get; set; } = default!;

		public ChatOverlay(IWebChatClient chatClient, IJSRuntime JS)
		{
			this.chatClient = chatClient;
			this.JS = JS;
			progress.ProgressChanged += OnWebLlmInitialization;

			Messages.Add(new ChatMessage(ChatRole.System, SystemPrompt)); // Add system prompt to the chat
		}

		private void ToggleChat()
		{
			IsOpen = !IsOpen;

			if (IsOpen && !IsInitialized)
			{
				IsInitialized = true; // Set to true to prevent re-initialization
				_ = InitializeLlmAsync();
			}
		}

		private async Task InitializeLlmAsync()
		{
			try
			{
				await chatClient.InitializeAsync(progress);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				throw;
			}
		}

		private void OnWebLlmInitialization(object? sender, InitializeProgress progress)
		{
			if (progress == null)
			{
				return;
			}

			lastProgress = progress;
			StateHasChanged();
		}

		private async Task StreamPromptRequest()
		{
			// Capture the user's input
			var input = CurrentMessage;

			// Add the user's message to the chat
			Messages.Add(new ChatMessage(ChatRole.User, input));
			CurrentMessage = ""; // Clear the input field
			IsBotTyping = true; // Indicate that the bot is typing
			StateHasChanged(); // Update the UI

			try
			{
				// Send the messages to the chat client and process the response
				await foreach (var response in chatClient.GetStreamingResponseAsync(Messages))
				{
					// Append the bot's streaming response
					streamingText += response.Text ?? "";
					StateHasChanged();

					// Scroll to the bottom of the chat
					await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
				}

				IsBotTyping = false;

				// Add the bot's final response to the chat
				Messages.Add(new ChatMessage(ChatRole.Assistant, streamingText));
				streamingText = string.Empty;

				StateHasChanged();

				// Scroll to the bottom of the chat
				await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during streaming: {ex.Message}");
			}
		}

		private MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

		private const string SystemPrompt = @"
		You are GhostfolioSidekick AI — a smart financial assistant. Help users understand and manage their investment portfolio.
		Respond clearly, avoid financial advice disclaimers, and answer in markdown with bullet points or tables when helpful. Also provide charts using mermaid markdown if helpful.
		Use financial terminology and suggest insights like trends or anomalies if data is present.";
	}
}