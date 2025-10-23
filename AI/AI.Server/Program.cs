using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Server
{
	internal static class Program
	{
		static async Task Main(string[] args)
		{
			using var chatClient = new ServerChatClient("https://huggingface.co/mradermacher/OpenChat-3.5-7B-Qwen-v2.0-i1-GGUF/resolve/main/OpenChat-3.5-7B-Qwen-v2.0.i1-Q4_K_M.gguf");
			await chatClient.InitializeAsync(null!);

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Hello, how are you?")
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
			Console.ReadLine();
		}
	}
}
