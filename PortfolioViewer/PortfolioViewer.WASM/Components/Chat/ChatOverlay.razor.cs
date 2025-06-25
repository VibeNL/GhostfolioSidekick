using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.PortfolioViewer.WASM.AI;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Chat
{
	public partial class ChatOverlay
	{
		private bool IsOpen = false;
		private string CurrentMessage = "";
		private bool IsBotTyping = false;
		private bool IsInitialized = false; // Flag to track initialization

		private IWebChatClient chatClient;

		private Progress<InitializeProgress> progress = new();
		private string streamingAuthor = string.Empty;
		private InitializeProgress lastProgress = new(0);

		private IJSRuntime JS { get; set; }

		private readonly List<ChatMessageContent> memory = new();
		private readonly AgentOrchestrator orchestrator;
		private readonly AgentLogger agentLogger;

		internal string CurrentAgentName => GhostfolioSidekick.PortfolioViewer.WASM.AI.AgentLogger.CurrentAgentName;

		public ChatOverlay(IWebChatClient chatClient, IJSRuntime JS, AgentOrchestrator agentOrchestrator, AgentLogger agentLogger)
		{
			orchestrator = agentOrchestrator;
			this.agentLogger = agentLogger;
			this.chatClient = chatClient;
			this.JS = JS;
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

		private async Task StreamPromptRequest()
		{
			// Capture the user's input
			var input = CurrentMessage;

			// Add the user's message to the chat
			CurrentMessage = ""; // Clear the input field
			IsBotTyping = true; // Indicate that the bot is typing
			StateHasChanged(); // Update the UI

			try
			{
				memory.AddRange(new ChatMessageContent(AuthorRole.User, input) { AuthorName = "User" });

				// Send the messages to the chat client and process the response
				await foreach (var response in orchestrator.AskQuestion(input))
				{
					// Append the bot's streaming response
					streamingAuthor = response.AuthorName ?? string.Empty;

					var lastMemory = memory.LastOrDefault();
					if (lastMemory?.AuthorName != streamingAuthor)
					{
						lastMemory = new ChatMessageContent(AuthorRole.Assistant, response.Content ?? string.Empty) { AuthorName = streamingAuthor };
						memory.Add(lastMemory);
					}

					lastMemory.Content += response.Content ?? string.Empty;
					StateHasChanged();

					// Scroll to the bottom of the chat
					await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
				}

				memory.Clear();
				memory.AddRange(await orchestrator.History());

				IsBotTyping = false;
				streamingAuthor = string.Empty;

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