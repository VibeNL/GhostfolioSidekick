using LLama.Common;
using LLama;

namespace GhostfolioSidekick.AI.Server
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			// Download model to a temp file instead of hardcoding the path.
			////string modelUrl = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf?download=true";
			var modelUrl = "https://huggingface.co/mradermacher/OpenChat-3.5-7B-Qwen-v2.0-i1-GGUF/resolve/main/OpenChat-3.5-7B-Qwen-v2.0.i1-Q4_K_M.gguf";

			////string tempModelPath = Path.Combine(Path.GetTempPath(), $"Phi-3-mini-4k-instruct-q4.gguf");
			string tempModelPath = Path.Combine(Path.GetTempPath(), GetNameFromModelUrl(modelUrl));

			// If the file already exists at that path (unlikely), skip download.
			if (!File.Exists(tempModelPath))
			{
				Console.WriteLine($"Downloading model to {tempModelPath} ...");
				using var http = new HttpClient();
				using var response = await http.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();

				await using var responseStream = await response.Content.ReadAsStreamAsync();
				await using var fileStream = File.Create(tempModelPath);
				await responseStream.CopyToAsync(fileStream);
				Console.WriteLine("Download complete.");
			}

			string modelPath = tempModelPath;

			try
			{
				var parameters = new ModelParams(modelPath)
				{
					ContextSize = 1024, // The longest length of chat as memory.
					GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
				};
				using var model = await LLamaWeights.LoadFromFileAsync(parameters);
				using var context = model.CreateContext(parameters);
				var executor = new InteractiveExecutor(context);

				// Add chat histories as prompt to tell AI how to act.
				var chatHistory = new ChatHistory();
				chatHistory.AddMessage(AuthorRole.System, "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
				chatHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
				chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

				ChatSession session = new(executor, chatHistory);

				InferenceParams inferenceParams = new InferenceParams()
				{
					MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
					AntiPrompts = new List<string> { "User:" } // Stop generation once antiprompts appear.
				};

				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("The chat session has started.\nUser: ");
				Console.ForegroundColor = ConsoleColor.Green;
				string userInput = Console.ReadLine() ?? "";

				while (userInput != "exit")
				{
					await foreach ( // Generate the response streamingly.
						var text
						in session.ChatAsync(
							new ChatHistory.Message(AuthorRole.User, userInput),
							inferenceParams))
					{
						Console.ForegroundColor = ConsoleColor.White;
						Console.Write(text);
					}
					Console.ForegroundColor = ConsoleColor.Green;
					userInput = Console.ReadLine() ?? "";
				}
			}
			finally
			{
				// Clean up the downloaded temp file. Comment this out if you want to keep the model cached.
				try
				{
					if (File.Exists(tempModelPath))
					{
						File.Delete(tempModelPath);
						Console.WriteLine($"Deleted temporary model file: {tempModelPath}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to delete temporary model file: {ex.Message}");
				}
			}
		}

		private static string GetNameFromModelUrl(string modelUrl)
		{
			if (string.IsNullOrWhiteSpace(modelUrl))
				throw new ArgumentException("modelUrl must not be null or empty", nameof(modelUrl));

			try
			{
				// Try to parse as a URI and get the last path segment
				var uri = new Uri(modelUrl);
				var fileName = Path.GetFileName(uri.AbsolutePath);
				if (!string.IsNullOrEmpty(fileName))
					return fileName;

				// If no filename could be extracted from the AbsolutePath, strip query/fragment and take the last segment
				var withoutQuery = modelUrl.Split(new[] { '?', '#' }, 2)[0];
				var lastSlash = withoutQuery.LastIndexOf('/');
				if (lastSlash >= 0 && lastSlash < withoutQuery.Length - 1)
					return withoutQuery.Substring(lastSlash + 1);

				// Fallback: create a deterministic filename based on SHA256 of the URL
				using var sha = System.Security.Cryptography.SHA256.Create();
				var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(modelUrl));
				var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
				return hex + ".gguf";
			}
			catch
			{
				// Last resort: use a GUID-based filename
				return Guid.NewGuid().ToString() + ".gguf";
			}
		}
	}
}