using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Wllama
{
	public class WllamaInteropInstance
	{
		private readonly IProgress<InitializeProgress> onProgress;

		public ConcurrentQueue<WllamaCompletion> WllamaCompletions { get; } = new();

		public WllamaInteropInstance(IProgress<InitializeProgress> onProgress)
		{
			this.onProgress = onProgress;
		}

		[JSInvokable]
		public Task ReportProgress(object progress)
		{
			try
			{
				if (progress is JsonElement jsonElement)
				{
					var progressObj = JsonSerializer.Deserialize<InitializeProgress>(jsonElement.GetRawText());
					if (progressObj != null)
					{
						onProgress.Report(progressObj);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reporting progress: {ex.Message}");
			}
			return Task.CompletedTask;
		}

		[JSInvokable]
		public Task ReceiveChunkCompletion(object completion)
		{
			try
			{
				if (completion is JsonElement jsonElement)
				{
					var completionObj = JsonSerializer.Deserialize<WllamaCompletion>(jsonElement.GetRawText());
					if (completionObj != null)
					{
						WllamaCompletions.Enqueue(completionObj);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error receiving chunk completion: {ex.Message}");
				// Enqueue error completion
				WllamaCompletions.Enqueue(new WllamaCompletion
				{
					Choices = new[]
					{
						new WllamaChoice
						{
							Delta = new WllamaDelta { Content = $"Error: {ex.Message}" }
						}
					},
					Done = true
				});
			}
			return Task.CompletedTask;
		}

		public object ConvertMessage(IList<Microsoft.Extensions.AI.ChatMessage> messages)
		{
			return messages.Select(m => new
			{
				role = m.Role.ToString().ToLowerInvariant(),
				content = m.Text ?? string.Empty
			}).ToArray();
		}
	}

	public class WllamaCompletion
	{
		public WllamaChoice[]? Choices { get; set; }
		public bool Done { get; set; }

		public bool IsStreamComplete => Done || (Choices?.Length == 0);
	}

	public class WllamaChoice
	{
		public WllamaDelta? Delta { get; set; }
	}

	public class WllamaDelta
	{
		public string? Content { get; set; }
	}
}