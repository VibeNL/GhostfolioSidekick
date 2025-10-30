using System.Text;
using GhostfolioSidekick.AI.Common;
using LLama;
using LLama.Common;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Server
{
	public sealed class ServerChatClient(string modelUrl) : ICustomChatClient
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
				OnProgress?.Report(new InitializeProgress(0.0, "Starting download"));

				using var http = new HttpClient();
				using var response = await http.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var contentLength = response.Content.Headers.ContentLength;

				await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				await using var fileStream = File.Create(tempModelPath);

				var buffer = new byte[81920];
				long totalRead =0;
				int read;

				while ((read = await responseStream.ReadAsync(buffer).ConfigureAwait(false)) >0)
				{
					await fileStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
					totalRead += read;

					double progress;
					string message;
					if (contentLength.HasValue && contentLength.Value >0)
					{
						progress = Math.Min(0.9,0.1 +0.8 * ((double)totalRead / contentLength.Value));
						message = $"Downloading model ({BytesToString(totalRead)} / {BytesToString(contentLength.Value)})";
					}
					else
					{
						// Unknown length - provide incremental progress based on bytes downloaded (cap at90%)
						progress = Math.Min(0.9,0.1 + Math.Min(0.8, (double)totalRead / (1024 *1024) *0.05));
						message = $"Downloading model ({BytesToString(totalRead)})";
					}

					OnProgress?.Report(new InitializeProgress(progress, message));
				}

				OnProgress?.Report(new InitializeProgress(0.95, "Finalizing model download"));
			}
			else
			{
				OnProgress?.Report(new InitializeProgress(0.5, "Model already downloaded"));
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

				var withoutQuery = modelUrl.Split(['?', '#'], 2)[0];
				var lastSlash = withoutQuery.LastIndexOf('/');
				if (lastSlash >= 0 && lastSlash < withoutQuery.Length - 1)
					return withoutQuery[(lastSlash + 1)..];
				var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(modelUrl));
				var hex = Convert.ToHexStringLower(hash);
				return hex + ".gguf";
			}
			catch
			{
				return Guid.NewGuid().ToString() + ".gguf";
			}
		}

		private static string BytesToString(long byteCount)
		{
			if (byteCount ==0) return "0B";
			string[] suf = ["B", "KB", "MB", "GB", "TB", "PB"];
			var bytes = Math.Abs((double)byteCount);
			var place = Convert.ToInt32(Math.Floor(Math.Log(bytes,1024)));
			var num = Math.Round(bytes / Math.Pow(1024, place),2);
			return (Math.Sign(byteCount) * num).ToString() + suf[place];
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