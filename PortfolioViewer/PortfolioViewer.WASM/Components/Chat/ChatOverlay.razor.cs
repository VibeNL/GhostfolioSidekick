using GhostfolioSidekick.AI.Agents;
using GhostfolioSidekick.AI.Common;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Chat
{
	public partial class ChatOverlay : ComponentBase, IDisposable
	{
		[Inject] private IJSRuntime JS { get; set; }

		private bool IsOpen;
		private string CurrentMessage = "";
		private bool IsBotTyping;
		private bool IsInitialized; // Flag to track initialization
		private string? initializationError; // Set when model initialization fails; shown in the loading panel with a retry option
		private bool wakeLockActive; // Track wake lock status

		private readonly Progress<InitializeProgress> progress = new();
		private string streamingAuthor = string.Empty;
		private InitializeProgress lastProgress = new(0);

		private readonly List<ChatMessage> memory = [];
		private readonly AgentOrchestrator orchestrator;
		private readonly AgentLogger agentLogger;
		private readonly SqlitePersistence sqlitePersistence;

		internal string CurrentAgentName => agentLogger.CurrentAgentName;

		internal string CurrentAgentFunction => agentLogger.CurrentAgentFunction;

		private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

		public ChatOverlay(IJSRuntime JS, AgentOrchestrator agentOrchestrator, AgentLogger agentLogger, SqlitePersistence sqlitePersistence)
		{
			orchestrator = agentOrchestrator;
			this.agentLogger = agentLogger;
			this.sqlitePersistence = sqlitePersistence;
			this.JS = JS;
			progress.ProgressChanged += OnWebLlmInitialization;

			// Subscribe to AgentLogger event
			agentLogger.CurrentAgentNameChanged += OnCurrentAgentNameChanged;
		}

		protected override async Task OnInitializedAsync()
		{
			// Restore the conversation from the database so it survives page refreshes.
			try
			{
				memory.AddRange(await orchestrator.HistoryAsync());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to restore chat history: {ex.Message}");
			}

			StateHasChanged();
		}

		private async Task ClearChat()
		{
			memory.Clear();
			await orchestrator.ClearMemoryAsync();
			CurrentMessage = string.Empty;

			// Persist the deletion so a page refresh does not resurrect the cleared conversation.
			try
			{
				await sqlitePersistence.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to sync chat history after clear: {ex.Message}");
			}
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
				initializationError = null;
				await orchestrator.InitializeAsync(progress);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				// Surface the failure in the loading panel instead of leaving "Loading assistant..." up forever.
				initializationError = e.Message;
				IsInitialized = false; // Allow a retry when the chat is reopened or Retry is pressed
				StateHasChanged();
			}
		}

		private bool initializationInProgress;

		private async Task RetryInitialization()
		{
			if (initializationInProgress)
			{
				return; // Guard against double-click starting two concurrent engine initializations
			}

			initializationInProgress = true;
			try
			{
				IsInitialized = true; // Prevent duplicate initialization while this retry runs
				lastProgress = new InitializeProgress(0);
				await InitializeLlmAsync();
			}
			finally
			{
				initializationInProgress = false;
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
				memory.Add(new ChatMessage(ChatRole.User, input) { AuthorName = "User" });

				// Send the messages to the chat client and process the response
				await foreach (var response in orchestrator.AskQuestion(input))
				{
					// Append the bot's streaming response
					streamingAuthor = response.AuthorName ?? string.Empty;

					var lastMemory = memory.LastOrDefault();
					if (lastMemory?.AuthorName != streamingAuthor)
					{
						lastMemory = new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty) { AuthorName = streamingAuthor };
						memory.Add(lastMemory);
					}

					var existingText = lastMemory.Text ?? string.Empty;
					lastMemory.Contents = [new TextContent(existingText + (response.Text ?? string.Empty))];
					StateHasChanged();

					// Scroll to the bottom of the chat
					await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
				}

				memory.Clear();
				memory.AddRange(await orchestrator.HistoryAsync());

				IsBotTyping = false;
				streamingAuthor = string.Empty;

				StateHasChanged();

				// Persist the new turn to IndexedDB so it survives a page refresh.
				try
				{
					await sqlitePersistence.SaveChangesAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to sync chat history: {ex.Message}");
				}

				// Scroll to the bottom of the chat
				await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
			}
			catch (Exception ex)
			{
				// Keep the conversation visible (prior turns, the failed question and any partial answer);
				// a failed turn is not persisted, so there is nothing to clear.
				memory.Add(new ChatMessage(ChatRole.System, $"Error: {ex.Message}") { AuthorName = "System" });

				// Unstick the UI: a failed stream must not leave the typing indicator up and input disabled.
				IsBotTyping = false;
				streamingAuthor = string.Empty;
				StateHasChanged();
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

		private static string GetBubbleStyle(ChatMessage? message)
		{
			const string baseStyle = "max-width: 85%; padding: 10px 14px; border-radius: 18px; font-size: 14px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); ";

			if (message?.Role == ChatRole.User)
			{
				return baseStyle + "background-color: #dbeafe; align-self: flex-end;";
			}
			else if (message?.Role == ChatRole.Assistant)
			{
				return baseStyle + "background-color: #e0f7fa; align-self: flex-start; border: 1px solid #b2ebf2;";
			}
			else if (message?.Role == ChatRole.System)
			{
				return baseStyle + "background-color: #f3e5f5; align-self: center; font-style: italic;";
			}
			else
			{
				return baseStyle + "background-color: #ffffff; border: 1px solid #e5e7eb; align-self: flex-start;";
			}
		}

		public void Dispose()
		{
			// Release wake lock on disposal — fire-and-forget is intentional here as Dispose cannot be async
			if (wakeLockActive)
			{
				_ = JS.InvokeAsync<bool>("wakeLockModule.releaseWakeLock");
				wakeLockActive = false;
			}

			// Unsubscribe from AgentLogger event
			agentLogger.CurrentAgentNameChanged -= OnCurrentAgentNameChanged;
		}
	}
}
