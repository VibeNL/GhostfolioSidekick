using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp
{
	public class ModelDownloadService
	{
		private readonly HttpClient httpClient;
		private readonly ILogger<ModelDownloadService> logger;
		
		// Phi-3 Mini model details
		private const string PHI3_MODEL_URL = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf";
		private const string PHI3_MODEL_FILENAME = "phi-3-mini-4k-instruct.Q4_0.gguf";
		private const long PHI3_MODEL_SIZE = 2_400_000_000; // Approximately 2.4GB

		public ModelDownloadService(HttpClient httpClient, ILogger<ModelDownloadService> logger)
		{
			this.httpClient = httpClient;
			this.logger = logger;
		}

		/// <summary>
		/// Downloads the Phi-3 Mini model if it doesn't exist
		/// </summary>
		/// <param name="targetPath">Target directory to download the model</param>
		/// <param name="progress">Progress reporter for download status</param>
		/// <returns>Full path to the downloaded model file</returns>
		public async Task<string> EnsureModelDownloadedAsync(string targetPath, IProgress<InitializeProgress>? progress = null)
		{
			try
			{
				// Ensure target directory exists
				Directory.CreateDirectory(targetPath);
				
				var modelFilePath = Path.Combine(targetPath, PHI3_MODEL_FILENAME);
				
				// Check if model already exists and has reasonable size
				if (File.Exists(modelFilePath))
				{
					var fileInfo = new FileInfo(modelFilePath);
					if (fileInfo.Length > PHI3_MODEL_SIZE * 0.9) // At least 90% of expected size
					{
						logger.LogInformation("Phi-3 Mini model already exists at {ModelPath}", modelFilePath);
						progress?.Report(new InitializeProgress(1.0, "Model already downloaded"));
						return modelFilePath;
					}
					else
					{
						logger.LogWarning("Existing model file appears incomplete, re-downloading...");
						File.Delete(modelFilePath);
					}
				}

				logger.LogInformation("Downloading Phi-3 Mini model from {Url} to {Path}", PHI3_MODEL_URL, modelFilePath);
				progress?.Report(new InitializeProgress(0.1, "Starting model download..."));

				// Download with progress tracking
				using var response = await httpClient.GetAsync(PHI3_MODEL_URL, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();

				var totalBytes = response.Content.Headers.ContentLength ?? PHI3_MODEL_SIZE;
				progress?.Report(new InitializeProgress(0.2, $"Downloading model ({totalBytes / 1_000_000}MB)..."));

				using var contentStream = await response.Content.ReadAsStreamAsync();
				using var fileStream = new FileStream(modelFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

				var buffer = new byte[8192];
				long totalBytesRead = 0;
				int bytesRead;

				while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await fileStream.WriteAsync(buffer, 0, bytesRead);
					totalBytesRead += bytesRead;

					// Report progress every MB to avoid too frequent updates
					if (totalBytesRead % (1024 * 1024) == 0 || totalBytesRead == totalBytes)
					{
						var progressPercent = Math.Min(0.2 + (double)totalBytesRead / totalBytes * 0.8, 0.99);
						var progressMessage = $"Downloading model: {totalBytesRead / 1_000_000}MB / {totalBytes / 1_000_000}MB";
						progress?.Report(new InitializeProgress(progressPercent, progressMessage));
					}
				}

				logger.LogInformation("Successfully downloaded Phi-3 Mini model to {ModelPath}, size: {Size}MB", 
					modelFilePath, totalBytesRead / 1_000_000);
				
				progress?.Report(new InitializeProgress(1.0, "Model download completed"));
				return modelFilePath;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to download Phi-3 Mini model");
				progress?.Report(new InitializeProgress(0.0, $"Model download failed: {ex.Message}"));
				throw;
			}
		}

		/// <summary>
		/// Gets the default model paths with Phi-3 Mini
		/// </summary>
		/// <param name="modelsDirectory">Base models directory</param>
		/// <returns>Dictionary mapping ChatMode to model paths</returns>
		public static Dictionary<ChatMode, string> GetDefaultModelPaths(string modelsDirectory = "wwwroot/models")
		{
			var modelPath = Path.Combine(modelsDirectory, PHI3_MODEL_FILENAME);
			return new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, modelPath },
				{ ChatMode.ChatWithThinking, modelPath },
				{ ChatMode.FunctionCalling, modelPath }
			};
		}

		/// <summary>
		/// Checks if Phi-3 Mini model exists and is valid
		/// </summary>
		/// <param name="modelPath">Path to check</param>
		/// <returns>True if model exists and appears valid</returns>
		public static bool IsModelAvailable(string modelPath)
		{
			if (!File.Exists(modelPath))
				return false;

			var fileInfo = new FileInfo(modelPath);
			return fileInfo.Length > PHI3_MODEL_SIZE * 0.9; // At least 90% of expected size
		}
	}
}