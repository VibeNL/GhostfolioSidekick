using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Server
{
	internal static class Program
	{
		private const string ModelUrl = "https://huggingface.co/tensorblock/Llama-3.2-8B-Instruct-GGUF/resolve/main/Llama-3.2-8B-Instruct-Q3_K_M.gguf";
		
		static async Task Main()
		{
			Progress<InitializeProgress> progress = new();
			progress.ProgressChanged += (s, e) =>
			{
				Console.WriteLine($"Initialization Progress: {e.Progress:P2} - {e.Message}");
			};

			using var chatClient = new ServerChatClient(ModelUrl);
			await chatClient.InitializeAsync(progress);

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Tell me a joke")
			};

			// Stream the response and print chunks as they arrive
			var sb = new System.Text.StringBuilder();
			await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
			{
				// Write chunks to the console without newlines to emulate streaming
				Console.Write(update.Text);
				sb.Append(update.Text);
			}

			// Print a newline after the stream completes and show the full response
			Console.WriteLine();
			Console.WriteLine("Full response:\n" + sb.ToString());
			Console.WriteLine("Enter to exit");
			Console.ReadLine();
		}
	}
}
