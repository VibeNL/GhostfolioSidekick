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

		[Inject] private IJSRuntime JS { get; set; } = default!;

		public ChatOverlay(IWebChatClient chatClient)
		{
			this.chatClient = chatClient;
			progress.ProgressChanged += OnWebLlmInitialization;
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


		//private async Task OnChunkCompletion(WebLLMCompletion response)
		//{
		//	if (response.IsStreamComplete)
		//	{
		//		IsBotTyping = false;

		//		var content = streamingText;
		//		Messages.Add(new Message("assistant", streamingText));
		//		streamingText = string.Empty;

		//		// Check if the response includes a function call
		//		if (content?.StartsWith("<function_call>") == true)
		//		{
		//			await HandleFunctionCall(content);
		//		}

		//		StateHasChanged();

		//		// Scroll to the bottom
		//		await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
		//	}
		//	else
		//	{
		//		streamingText += response.Choices?.ElementAtOrDefault(0)?.Delta?.Content ?? "";
		//	}

		//	StateHasChanged();
		//	// Scroll to the bottom
		//	await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
		//	await Task.CompletedTask;
		//}

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
	}
}