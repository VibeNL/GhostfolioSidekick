using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Chat
{
	public partial class ChatOverlay : ComponentBase, IDisposable
	{
		[Inject] private IJSRuntime JS { get; set; }

		private bool IsOpen;
		private string CurrentMessage = "";
		private bool IsBotTyping;
		private bool IsInitialized; // Flag to track initialization
		private bool wakeLockActive; // Track wake lock status

		private readonly IWebChatClient chatClient;

		private readonly Progress<InitializeProgress> progress = new();
		private string streamingAuthor = string.Empty;
		private InitializeProgress lastProgress = new(0);

		private readonly List<ChatMessageContent> memory = [];
		private readonly AgentOrchestrator orchestrator;
		private readonly AgentLogger agentLogger;

		internal string CurrentAgentName => agentLogger.CurrentAgentName;

		internal string CurrentAgentFunction => agentLogger.CurrentAgentFunction;

		private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

		public ChatOverlay(IWebChatClient chatClient, IJSRuntime JS, AgentOrchestrator agentOrchestrator, AgentLogger agentLogger)
		{
			orchestrator = agentOrchestrator;
			this.agentLogger = agentLogger;
			this.chatClient = chatClient;
			this.JS = JS;
			progress.ProgressChanged += OnWebLlmInitialization;

			// Subscribe to AgentLogger event
			agentLogger.CurrentAgentNameChanged += OnCurrentAgentNameChanged;
		}

		private async Task ToggleChat()
		{
			IsOpen = !IsOpen;

			if (IsOpen)
			{
				// Request wake lock when chat is opened
				await RequestWakeLock();

				if (!IsInitialized)
				{
					IsInitialized = true; // Set to true to prevent re-initialization
					_ = InitializeLlmAsync();
				}
			}
			else
			{
				// Release wake lock when chat is closed
				await ReleaseWakeLock();
			}
		}

		private async Task RequestWakeLock()
		{
			try
			{
				var result = await JS.InvokeAsync<bool>("wakeLockModule.requestWakeLock");
				wakeLockActive = result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error requesting wake lock: {ex.Message}");
			}

			StateHasChanged();
		}

		private async Task ReleaseWakeLock()
		{
			try
			{
				if (wakeLockActive)
				{
					_ = await JS.InvokeAsync<bool>("wakeLockModule.releaseWakeLock");
					wakeLockActive = false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error releasing wake lock: {ex.Message}");
			}

			StateHasChanged();
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

		private void OnCurrentAgentNameChanged()
		{
			InvokeAsync(StateHasChanged);
		}

		private async Task HandleInputKeyUp(KeyboardEventArgs e)
		{
			if (e.Key == "Enter" && !IsBotTyping)
			{
				await StreamPromptRequest();
			}
		}

		private static string GetBubbleStyle(ChatMessageContent? message)
		{
			const string baseStyle = "max-width: 85%; padding: 10px 14px; border-radius: 18px; font-size: 14px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); ";

			if (message?.Role == AuthorRole.User)
			{
				return baseStyle + "background-color: #dbeafe; align-self: flex-end;";
			}
			else if (message?.Role == AuthorRole.Assistant)
			{
				return baseStyle + "background-color: #e0f7fa; align-self: flex-start; border: 1px solid #b2ebf2;";
			}
			else if (message?.Role == AuthorRole.System)
			{
				return baseStyle + "background-color: #f3e5f5; align-self: center; font-style: italic;";
			}
			else
			{
				return baseStyle + "background-color: #ffffff; border: 1px solid #e5e7eb; align-self: flex-start;";
			}
		}

		public async void Dispose()
		{
			// Release wake lock on disposal
			await ReleaseWakeLock();

			// Unsubscribe from AgentLogger event
			agentLogger.CurrentAgentNameChanged -= OnCurrentAgentNameChanged;
		}
	}
}
