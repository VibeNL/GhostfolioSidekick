using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Api;

public class ApiChatClient : ICustomChatClient, IDisposable
{
	private readonly HttpClient httpClient;
	private readonly ILogger<ApiChatClient> logger;
	private bool disposed;

	public ApiChatClient(HttpClient httpClient, ILogger<ApiChatClient> logger)
	{
		this.httpClient = httpClient;
		this.logger = logger;
	}

	public ChatMode ChatMode { get; set; } = ChatMode.Chat;

	public object? GetService(Type serviceType, object? serviceKey = null) => this;

	public ICustomChatClient Clone()
	{
		return new ApiChatClient(httpClient, logger)
		{
			ChatMode = this.ChatMode
		};
	}

	public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{
		var request = new ApiChatRequest
		{
			ChatMode = ChatMode,
			Messages = messages.Select(m => new ApiChatMessage { Role = m.Role.ToString(), Text = m.Text }).ToList()
		};

		var httpResponse = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
		httpResponse.EnsureSuccessStatusCode();

		var resp = await httpResponse.Content.ReadFromJsonAsync<ApiChatResponse>(cancellationToken: cancellationToken);
		var text = resp?.Text ?? string.Empty;

		return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// Non-streaming fallback: call API and yield single update
		var request = new ApiChatRequest
		{
			ChatMode = ChatMode,
			Messages = messages.Select(m => new ApiChatMessage { Role = m.Role.ToString(), Text = m.Text }).ToList()
		};

		var httpResponse = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
		httpResponse.EnsureSuccessStatusCode();

		var resp = await httpResponse.Content.ReadFromJsonAsync<ApiChatResponse>(cancellationToken: cancellationToken);
		var text = resp?.Text ?? string.Empty;

		yield return new ChatResponseUpdate(ChatRole.Assistant, text);
	}

	public Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
	{
		// No initialization required for API backed client
		OnProgress?.Report(new InitializeProgress(1.0, "API chat client initialized"));
		return Task.CompletedTask;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposed) return;
		if (disposing)
		{
			// no managed resources to dispose
		}
		disposed = true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~ApiChatClient()
	{
		Dispose(false);
	}

	// Local DTOs used by the client
	private class ApiChatRequest
	{
		public ChatMode ChatMode { get; set; }
		public List<ApiChatMessage> Messages { get; set; } = new();
	}

	private class ApiChatMessage
	{
		public string Role { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
	}

	private class ApiChatResponse
	{
		public string Text { get; set; } = string.Empty;
	}
}
