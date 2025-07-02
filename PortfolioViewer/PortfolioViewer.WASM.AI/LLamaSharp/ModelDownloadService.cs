using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp
{
	public class ModelDownloadService
	{
		private readonly HttpClient httpClient;
		private readonly ILogger<ModelDownloadService> logger;
		private readonly IJSRuntime? jsRuntime;
		
		// Phi-3 Mini model details
		private const string PHI3_MODEL_URL = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf";
		private const string PHI3_MODEL_FILENAME = "phi-3-mini-4k-instruct.Q4_0.gguf";
		private const long PHI3_MODEL_SIZE = 2_400_000_000; // Approximately 2.4GB
		private const string BROWSER_STORAGE_KEY = "llama_model_phi3_mini";

		public ModelDownloadService(HttpClient httpClient, ILogger<ModelDownloadService> logger, IJSRuntime? jsRuntime = null)
		{
			this.httpClient = httpClient;
			this.logger = logger;
			this.jsRuntime = jsRuntime;
		}

		/// <summary>
		/// Downloads the Phi-3 Mini model if it doesn't exist
		/// In WASM, uses IndexedDB for storage and proxy for download
		/// </summary>
		/// <param name="targetPath">Target directory to download the model (ignored in WASM)</param>
		/// <param name="progress">Progress reporter for download status</param>
		/// <returns>Full path to the downloaded model file</returns>
		public async Task<string> EnsureModelDownloadedAsync(string targetPath, IProgress<InitializeProgress>? progress = null)
		{
			try
			{
				var isWasm = IsWasmEnvironment();
				
				if (isWasm && jsRuntime != null)
				{
					return await EnsureModelDownloadedWasmAsync(progress);
				}
				else if (!isWasm)
				{
					return await EnsureModelDownloadedServerAsync(targetPath, progress);
				}
				else
				{
					// WASM without JSRuntime - not supported
					throw new NotSupportedException(
						"LLamaSharp requires browser storage APIs that are not available. " +
						"Please use WebLLM for browser-based AI.");
				}
			}
			catch (NotSupportedException ex)
			{
				// This is expected for WASM environments with large models
				logger.LogWarning(ex, "Model download not supported in current environment");
				var errorMessage = IsWasmEnvironment() 
					? "Large model download not supported in browser environment. WebLLM will be used instead."
					: ex.Message;
				
				progress?.Report(new InitializeProgress(0.0, errorMessage));
				throw;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to download Phi-3 Mini model");
				var errorMessage = IsWasmEnvironment() 
					? "Model download failed in browser environment. WebLLM will be used instead."
					: $"Model download failed: {ex.Message}";
				
				progress?.Report(new InitializeProgress(0.0, errorMessage));
				throw;
			}
		}

		/// <summary>
		/// Download and store model in browser using IndexedDB with chunked downloads
		/// </summary>
		private async Task<string> EnsureModelDownloadedWasmAsync(IProgress<InitializeProgress>? progress = null)
		{
			progress?.Report(new InitializeProgress(0.1, "Checking browser storage for model..."));
			
			// Check if model exists in IndexedDB
			var modelExists = await jsRuntime!.InvokeAsync<bool>("blazorBrowserStorage.hasModel", BROWSER_STORAGE_KEY);
			
			if (modelExists)
			{
				// Verify the model size in storage
				var storedSize = await jsRuntime.InvokeAsync<long>("blazorBrowserStorage.getModelSize", BROWSER_STORAGE_KEY);
				if (storedSize > PHI3_MODEL_SIZE * 0.9) // At least 90% of expected size
				{
					logger.LogInformation("Phi-3 Mini model already exists in browser storage, size: {Size}MB", storedSize / 1_000_000);
					progress?.Report(new InitializeProgress(1.0, "Model found in browser storage"));
					
					// Create virtual file path for WASM
					var existingModelPath = $"/models/{PHI3_MODEL_FILENAME}";
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.mountModel", BROWSER_STORAGE_KEY, existingModelPath);
					return existingModelPath;
				}
				else
				{
					logger.LogWarning("Existing model in browser storage appears incomplete, re-downloading...");
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.deleteModel", BROWSER_STORAGE_KEY);
				 }
			 }

			// Check if we have a proper base address configured
			if (httpClient.BaseAddress == null)
			{
				logger.LogError("HttpClient BaseAddress is null. This indicates a service discovery or configuration issue.");
				progress?.Report(new InitializeProgress(0.0, 
					"HttpClient not properly configured. Check service discovery configuration and ensure API service is running."));
				
				throw new InvalidOperationException(
					"HttpClient BaseAddress is not set. This typically indicates that service discovery is not properly " +
					"configured or the API service is not accessible. Please check the Aspire configuration and ensure " +
					"PortfolioViewer.ApiService is running and discoverable.");
			 }

			// Log HttpClient information for debugging
			var baseAddress = httpClient.BaseAddress.ToString();
			logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", baseAddress);

			// Download model through proxy API using chunked approach
			logger.LogInformation("Downloading Phi-3 Mini model via chunked proxy API");
			progress?.Report(new InitializeProgress(0.2, "Starting chunked model download..."));

			// Test if the proxy endpoint is reachable first
			await TestProxyEndpointAsync(baseAddress);

			// Use chunked download approach to avoid browser limitations
			return await DownloadModelInChunksAsync(progress);
		}

		/// <summary>
		/// Test if the proxy endpoint is accessible
		/// </summary>
		private async Task TestProxyEndpointAsync(string baseAddress)
		{
			try
			{
				var testUrl = new Uri(httpClient.BaseAddress!, "api/proxy/fetch?url=https://httpbin.org/get").ToString();
				logger.LogInformation("Testing proxy endpoint: {TestUrl}", testUrl);
				
				var testResponse = await httpClient.GetAsync(testUrl);
				
				if (!testResponse.IsSuccessStatusCode)
				{
					var errorDetails = testResponse.StatusCode switch
					{
						System.Net.HttpStatusCode.NotFound => "Proxy endpoint not found. Check that ProxyController is properly configured.",
						System.Net.HttpStatusCode.ServiceUnavailable => "API service is not available.",
						System.Net.HttpStatusCode.Unauthorized => "Authentication required for API service.",
						_ => $"Unexpected response: {testResponse.StatusCode}"
					};
					
					throw new HttpRequestException($"Proxy endpoint not accessible: {errorDetails}");
				}
				
				logger.LogInformation("Proxy endpoint test successful");
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "Proxy endpoint test failed");
				throw new NotSupportedException(
					$"Model download requires the API service to be running for CORS proxy support. " +
					$"Proxy test failed: {ex.Message}. Please ensure the PortfolioViewer.ApiService is running and accessible, " +
					$"or use WebLLM for browser-based AI.");
			 }
		}

		/// <summary>
		/// Download model using chunked approach to avoid browser size limitations
		/// </summary>
		private async Task<string> DownloadModelInChunksAsync(IProgress<InitializeProgress>? progress = null)
		{
			const long CHUNK_SIZE = 100 * 1024 * 1024; // 100MB chunks - well under any browser limit
			const int MAX_RETRIES = 3;
			
			progress?.Report(new InitializeProgress(0.25, "Starting chunked download (100MB chunks)..."));

			// Initialize the storage for chunked download
			await jsRuntime!.InvokeVoidAsync("blazorBrowserStorage.initializeModelDownload", BROWSER_STORAGE_KEY, PHI3_MODEL_SIZE);

			long totalBytesDownloaded = 0;
			long currentOffset = 0;
			int chunkIndex = 0;

			try
			{
				while (currentOffset < PHI3_MODEL_SIZE)
				{
					var endOffset = Math.Min(currentOffset + CHUNK_SIZE - 1, PHI3_MODEL_SIZE - 1);
					var chunkSize = endOffset - currentOffset + 1;
					
					logger.LogInformation("Downloading chunk {ChunkIndex}: bytes {Start}-{End} ({Size}MB)", 
						chunkIndex, currentOffset, endOffset, chunkSize / 1_000_000);

					// Download this chunk with retries
					byte[] chunkData = null!;
					for (int retry = 0; retry < MAX_RETRIES; retry++)
					{
						try
						{
							chunkData = await DownloadChunkAsync(currentOffset, endOffset);
							break; // Success
						}
						catch (Exception ex) when (retry < MAX_RETRIES - 1)
						{
							logger.LogWarning(ex, "Chunk {ChunkIndex} download failed, retry {Retry}/{MaxRetries}", 
								chunkIndex, retry + 1, MAX_RETRIES);
							await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry))); // Exponential backoff
						}
					}

					if (chunkData == null)
					{
						throw new InvalidOperationException($"Failed to download chunk {chunkIndex} after {MAX_RETRIES} retries");
					}

					// Store chunk in IndexedDB
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.appendModelChunk", BROWSER_STORAGE_KEY, chunkData);
					
					totalBytesDownloaded += chunkData.Length;
					currentOffset = endOffset + 1;
					chunkIndex++;

					// Update progress
					var progressPercent = 0.25 + (double)totalBytesDownloaded / PHI3_MODEL_SIZE * 0.65;
					var progressMessage = $"Downloaded {totalBytesDownloaded / 1_000_000}MB / {PHI3_MODEL_SIZE / 1_000_000}MB ({chunkIndex} chunks)";
					progress?.Report(new InitializeProgress(progressPercent, progressMessage));

					// Small delay to prevent overwhelming the browser
					await Task.Delay(100);
				}

				// Finalize the storage
				await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.finalizeModelDownload", BROWSER_STORAGE_KEY);

				logger.LogInformation("Successfully downloaded Phi-3 Mini model in {ChunkCount} chunks, total size: {Size}MB", 
					chunkIndex, totalBytesDownloaded / 1_000_000);
				
				progress?.Report(new InitializeProgress(0.95, "Mounting model in virtual file system..."));

				// Mount the model to virtual file system
				var downloadedModelPath = $"/models/{PHI3_MODEL_FILENAME}";
				await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.mountModel", BROWSER_STORAGE_KEY, downloadedModelPath);
				
				progress?.Report(new InitializeProgress(1.0, "Chunked model download completed successfully"));
				return downloadedModelPath;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Chunked model download failed at chunk {ChunkIndex}", chunkIndex);
				progress?.Report(new InitializeProgress(0.0, 
					$"Chunked download failed at chunk {chunkIndex}. Using WebLLM instead."));
				
				// Clean up partial download
				try
				{
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.deleteModel", BROWSER_STORAGE_KEY);
				}
				catch (Exception cleanupEx)
				{
					logger.LogWarning(cleanupEx, "Failed to clean up partial download");
				}

				throw new NotSupportedException(
					$"Chunked model download failed after downloading {totalBytesDownloaded / 1_000_000}MB " +
					$"in {chunkIndex} chunks. This may indicate network issues or browser limitations. " +
					$"WebLLM will be used instead, which doesn't require large downloads.");
			}
		}

		/// <summary>
		/// Download a specific chunk using HTTP Range requests via proxy
		/// </summary>
		private async Task<byte[]> DownloadChunkAsync(long startOffset, long endOffset)
		{
			// Create proxy URL with range parameters
			var rangeProxyUrl = new Uri(httpClient.BaseAddress!, 
				$"api/proxy/download-model-range?url={Uri.EscapeDataString(PHI3_MODEL_URL)}&start={startOffset}&end={endOffset}");

			using var response = await httpClient.GetAsync(rangeProxyUrl);
			response.EnsureSuccessStatusCode();

			// Verify we got the expected range
			if (response.StatusCode != System.Net.HttpStatusCode.PartialContent && 
				response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new InvalidOperationException($"Unexpected response status for range request: {response.StatusCode}");
			}

			var chunkData = await response.Content.ReadAsByteArrayAsync();
			
			// Verify chunk size
			var expectedSize = endOffset - startOffset + 1;
			if (chunkData.Length != expectedSize)
			{
				logger.LogWarning("Chunk size mismatch: expected {Expected}, got {Actual}", expectedSize, chunkData.Length);
			}

			return chunkData;
		}

		/// <summary>
		/// Download model on server/desktop using traditional file system
		/// </summary>
		private async Task<string> EnsureModelDownloadedServerAsync(string targetPath, IProgress<InitializeProgress>? progress = null)
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

			// Download directly (works only on server-side)
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

		/// <summary>
		/// Detects if running in WebAssembly environment
		/// </summary>
		private static bool IsWasmEnvironment()
		{
			// Check for WASM-specific indicators
			return System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
				   Environment.OSVersion.Platform == PlatformID.Other ||
				   Type.GetType("System.Runtime.InteropServices.JavaScript.JSHost") != null;
		}

		/// <summary>
		/// Gets the default model paths with Phi-3 Mini
		/// </summary>
		/// <param name="modelsDirectory">Base models directory</param>
		/// <returns>Dictionary mapping ChatMode to model paths</returns>
		public static Dictionary<ChatMode, string> GetDefaultModelPaths(string modelsDirectory = "wwwroot/models")
		{
			var modelPath = IsWasmEnvironment() 
				? $"/models/{PHI3_MODEL_FILENAME}" // Virtual path in WASM
				: Path.Combine(modelsDirectory, PHI3_MODEL_FILENAME); // Physical path on server

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
			try
			{
				if (IsWasmEnvironment())
				{
					// In WASM, we can't easily check file existence without async calls
					// This will be handled by the download service directly
					return false;
				}

				if (!File.Exists(modelPath))
					return false;

				var fileInfo = new FileInfo(modelPath);
				return fileInfo.Length > PHI3_MODEL_SIZE * 0.9; // At least 90% of expected size
			}
			catch
			{
				// File system access failed
				return false;
			}
		}
	}
}