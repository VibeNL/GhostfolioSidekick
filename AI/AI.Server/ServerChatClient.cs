using System.Text;
using GhostfolioSidekick.AI.Common;
using LLama;
using LLama.Common;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Server
{
	public sealed class ServerChatClient(string modelUrl) : ICustomChatClient, IDisposable
	{
		private readonly string modelUrl = modelUrl ?? throw new ArgumentNullException(nameof(modelUrl));
		private readonly string tempModelPath = Path.Combine(Path.GetTempPath(), GetNameFromModelUrl(modelUrl));
		private LLamaWeights? model;
		private LLamaContext? context;
		private InteractiveExecutor? executor;

		public ChatMode ChatMode { get; set; } = ChatMode.Chat;

		public object? GetService(Type serviceType, object? serviceKey = null) => this;

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			// Download the model if required
			if (!File.Exists(tempModelPath))
			{
				using var http = new HttpClient();
				using var response = await http.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				await using var fileStream = File.Create(tempModelPath);
				await responseStream.CopyToAsync(fileStream).ConfigureAwait(false);
			}

			// Load model into LLama
			var parameters = new ModelParams(tempModelPath)
			{
				ContextSize =1024,
				GpuLayerCount =0
			};

			model = await LLamaWeights.LoadFromFileAsync(parameters).ConfigureAwait(false);
			context = model.CreateContext(parameters);
			executor = new InteractiveExecutor(context);

			// Report initialization done
			OnProgress?.Report(new InitializeProgress(1.0, "Model loaded"));
		}

		public ICustomChatClient Clone()
		{
			// Create a new instance that shares the loaded model/context if available
			var clone = new ServerChatClient(modelUrl)
			{
				ChatMode = this.ChatMode,
				model = this.model,
				context = this.context,
				executor = this.executor
			};
			return clone;
		}

		public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			var sb = new StringBuilder();
			await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken))
			{
				sb.Append(update.Text);
			}

			return new ChatResponse(new ChatMessage(ChatRole.Assistant, sb.ToString()));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (executor == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
				yield break;
			}

			var input = string.Join("\n", messages.Where(m => !string.IsNullOrWhiteSpace(m.Text)).Select(m => m.Text));

			// Use ChatSession + InteractiveExecutor to generate text stream
			var chatHistory = new ChatHistory();
			// Optionally add previous messages into chatHistory here if needed

			var session = new ChatSession(executor, chatHistory);

			var inference = new InferenceParams()
			{
				MaxTokens =256,
				AntiPrompts = new List<string> { "User:" }
			};

			await foreach (var text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input), inference, cancellationToken))
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, text);
			}
		}

		private static string GetNameFromModelUrl(string modelUrl)
		{
			if (string.IsNullOrWhiteSpace(modelUrl))
				throw new ArgumentException("modelUrl must not be null or empty", nameof(modelUrl));

			try
			{
				var uri = new Uri(modelUrl);
				var fileName = Path.GetFileName(uri.AbsolutePath);
				if (!string.IsNullOrEmpty(fileName))
					return fileName;

				var withoutQuery = modelUrl.Split(new[] { '?', '#' },2)[0];
				var lastSlash = withoutQuery.LastIndexOf('/');
				if (lastSlash >=0 && lastSlash < withoutQuery.Length -1)
					return withoutQuery[(lastSlash +1)..];

				using var sha = System.Security.Cryptography.SHA256.Create();
				var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(modelUrl));
				var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
				return hex + ".gguf";
			}
			catch
			{
				return Guid.NewGuid().ToString() + ".gguf";
			}
		}

		public void Dispose()
		{
			try
			{
				context?.Dispose();
				model?.Dispose();
			}
			catch
			{
				// ignore
			}
			GC.SuppressFinalize(this);
		}
	}
}