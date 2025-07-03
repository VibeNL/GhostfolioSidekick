using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp
{
	public class ModelDownloadService
	{
		private readonly HttpClient httpClient;
		private readonly ILogger<ModelDownloadService> logger;
		public readonly IJSRuntime? jsRuntime; // Make this public so LLamaSharpChatClient can access it

		public ModelDownloadService(HttpClient httpClient, ILogger<ModelDownloadService> logger, IJSRuntime? jsRuntime = null)
		{
			this.httpClient = httpClient;
			this.logger = logger;
			this.jsRuntime = jsRuntime;
		}

		/// <summary>
		 /// Gets the storage key for a given model filename (for WASM environments)
		 /// </summary>
		 /// <param name="modelFilename">The model filename</param>
		 /// <returns>The storage key used in IndexedDB</returns>
		public static string GetStorageKeyForModel(string modelFilename)
		{
			return AIModelConstants.GetStorageKeyForModel(modelFilename);
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
					// In WASM environment, first ensure we have the WASM backend
					await EnsureWasmBackendAvailableAsync(progress);
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
					? "LLamaSharp WASM backend not available. WebLLM will be used instead."
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
		/// Ensures the WASM backend for LLamaSharp is available
		/// </summary>
		private async Task EnsureWasmBackendAvailableAsync(IProgress<InitializeProgress>? progress = null)
		{
			progress?.Report(new InitializeProgress(0.05, "Checking LLamaSharp WASM backend..."));
			
			try
			{
				// Check if WASM backend is already loaded
				var backendLoaded = await jsRuntime!.InvokeAsync<bool>("llamaSharpWasm.isBackendLoaded");
				
				if (backendLoaded)
				{
					logger.LogInformation("LLamaSharp WASM backend already loaded");
					return;
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to check WASM backend status, assuming not loaded");
				// Continue with loading attempt
			}

			progress?.Report(new InitializeProgress(0.1, "Loading LLamaSharp WASM backend..."));
			
			// Load the WASM backend
			try
			{
				await jsRuntime!.InvokeVoidAsync("llamaSharpWasm.loadBackend");
				
				// Wait for backend to be ready with timeout
				var maxWaitTime = TimeSpan.FromMinutes(2);
				var startTime = DateTime.UtcNow;
				bool isLoaded = false;
				
				while (!isLoaded && DateTime.UtcNow - startTime < maxWaitTime)
				{
					try
					{
						isLoaded = await jsRuntime.InvokeAsync<bool>("llamaSharpWasm.isBackendLoaded");
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Error checking backend load status, retrying...");
						await Task.Delay(1000);
						continue;
					}
					
					if (!isLoaded)
					{
						await Task.Delay(500);
					}
				}
				
				if (!isLoaded)
				{
					throw new TimeoutException("WASM backend loading timed out after 2 minutes");
				}
				
				logger.LogInformation("LLamaSharp WASM backend loaded successfully");
				progress?.Report(new InitializeProgress(0.15, "LLamaSharp WASM backend ready"));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load LLamaSharp WASM backend");
				throw new NotSupportedException(
					$"LLamaSharp WASM backend could not be loaded: {ex.Message}. " +
					"This might be due to missing WASM files or browser compatibility issues. " +
					"WebLLM will be used as fallback.");
			}
		}

		/// <summary>
		/// Download and store model in browser using IndexedDB with chunked downloads
		/// </summary>
		private async Task<string> EnsureModelDownloadedWasmAsync(IProgress<InitializeProgress>? progress = null)
		{
			progress?.Report(new InitializeProgress(0.05, "Checking browser storage availability..."));
			
			// First ensure browser storage is available and initialized
			await EnsureBrowserStorageInitializedAsync();
			
			progress?.Report(new InitializeProgress(0.1, "Checking browser storage for model..."));
			
			// Get model configuration
			var modelConfig = AIModelConstants.GetModelConfig(AIModelConstants.Models.Phi3Mini4K.Filename) 
				?? throw new InvalidOperationException("Phi-3 Mini model configuration not found");
			
			logger.LogInformation("Checking for model in browser storage with key: {StorageKey}", modelConfig.BrowserStorageKey);
			
			// Check if model exists in IndexedDB
			bool modelExists = false;
			try
			{
				modelExists = await jsRuntime!.InvokeAsync<bool>("blazorBrowserStorage.hasModel", modelConfig.BrowserStorageKey);
				logger.LogInformation("Model exists check result: {Exists} for storage key: {StorageKey}", modelExists, modelConfig.BrowserStorageKey);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to check if model exists in browser storage with key '{StorageKey}', assuming it doesn't exist", modelConfig.BrowserStorageKey);
				modelExists = false;
			}
			
			if (modelExists)
			{
				try
				{
					// Verify the model size in storage
					var storedSize = await jsRuntime.InvokeAsync<long>("blazorBrowserStorage.getModelSize", modelConfig.BrowserStorageKey);
					logger.LogInformation("Found model in storage with size: {Size}MB (expected: {ExpectedSize}MB)", 
						storedSize / 1_000_000, modelConfig.SizeBytes / 1_000_000);
						
					if (storedSize > modelConfig.SizeBytes * 0.9) // At least 90% of expected size
					{
						logger.LogInformation("Phi-3 Mini model already exists in browser storage, size: {Size}MB", storedSize / 1_000_000);
						progress?.Report(new InitializeProgress(1.0, "Model found in browser storage"));
						
						logger.LogInformation("Attempting to mount model: StorageKey='{StorageKey}', VirtualPath='{VirtualPath}', Filename='{Filename}'", 
							modelConfig.BrowserStorageKey, modelConfig.VirtualPath, modelConfig.Filename);
						
						try
						{
							await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.mountModel", modelConfig.BrowserStorageKey, modelConfig.VirtualPath);
							logger.LogInformation("Successfully mounted existing model from storage key '{StorageKey}' to path '{ModelPath}'", 
								modelConfig.BrowserStorageKey, modelConfig.VirtualPath);
							return modelConfig.VirtualPath;
						}
						catch (Exception mountEx)
						{
							logger.LogWarning(mountEx, "Failed to mount existing model from storage key '{StorageKey}', will re-download", modelConfig.BrowserStorageKey);
							// Continue with download
						}
					}
					else
					{
						logger.LogWarning("Existing model in browser storage appears incomplete (size: {Size}MB), re-downloading...", storedSize / 1_000_000);
						try
						{
							await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.deleteModel", modelConfig.BrowserStorageKey);
							logger.LogInformation("Deleted incomplete model with storage key: {StorageKey}", modelConfig.BrowserStorageKey);
						}
						catch (Exception deleteEx)
						{
							logger.LogWarning(deleteEx, "Failed to delete incomplete model with storage key '{StorageKey}', continuing anyway", modelConfig.BrowserStorageKey);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to verify existing model with storage key '{StorageKey}', will attempt re-download", modelConfig.BrowserStorageKey);
					// Continue with download
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
			await TestProxyEndpointAsync(baseAddress, modelConfig);

			// Use chunked download approach to avoid browser limitations
			return await DownloadModelInChunksAsync(progress, modelConfig);
		}

		/// <summary>
		 /// Ensure browser storage is available and initialized
		 /// </summary>
		private async Task EnsureBrowserStorageInitializedAsync()
		{
			try
			{
				// First check if browser storage is ready
				bool isReady = false;
				try
				{
					isReady = await jsRuntime!.InvokeAsync<bool>("checkBrowserStorageReady");
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to check browser storage readiness, attempting initialization");
				}

				if (!isReady)
				{
					logger.LogInformation("Browser storage not ready, attempting to initialize...");
					
					// Try to initialize browser storage
					var initialized = await jsRuntime!.InvokeAsync<bool>("initializeBrowserStorage");
					
					if (!initialized)
					{
						// Try debugging to understand why initialization failed
						try
						{
							await jsRuntime.InvokeVoidAsync("debugBrowserStorage");
						}
						catch (Exception debugEx)
						{
							logger.LogWarning(debugEx, "Failed to get browser storage debug info");
						}
						
						throw new InvalidOperationException(
							"Failed to initialize browser storage system. This could be due to: " +
							"1) IndexedDB not being available in the browser, " +
							"2) Insufficient browser permissions for storage, " +
							"3) Browser storage quota exceeded, or " +
							"4) Browser incompatibility. " +
							"Please try clearing browser data or using a different browser.");
					}
					
					logger.LogInformation("Browser storage initialized successfully");
				}
				else
				{
					logger.LogInformation("Browser storage is already ready");
				}

				// Double-check that the essential functions are available
				try
				{
					// Test if the required functions exist
					await jsRuntime!.InvokeAsync<bool>("eval", "typeof blazorBrowserStorage !== 'undefined' && typeof blazorBrowserStorage.initializeModelDownload === 'function'");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Browser storage functions are not properly available");
					throw new InvalidOperationException(
						"Browser storage JavaScript functions are not available. " +
						"This indicates a script loading issue. Please refresh the page and try again.");
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to ensure browser storage is initialized");
				throw new InvalidOperationException($"Browser storage initialization failed: {ex.Message}. " +
					"This feature requires a modern browser with IndexedDB support. " +
					"WebLLM will be used as a fallback for AI features.", ex);
			}
		}

		/// <summary>
		/// Test if the proxy endpoint is accessible
		/// </summary>
		private async Task TestProxyEndpointAsync(string baseAddress, ModelConfig modelConfig)
		{
			try
			{
				// Test the basic fetch endpoint first
				var testUrl = new Uri(httpClient.BaseAddress!, "api/proxy/fetch?url=https://httpbin.org/get").ToString();
				logger.LogInformation("Testing basic proxy endpoint: {TestUrl}", testUrl);
				
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
				
				logger.LogInformation("Basic proxy endpoint test successful");

				// Test if the model URL is accessible via HEAD request
				await TestModelAccessibilityAsync(modelConfig);

				// Test the specific download-model-range endpoint with a small range from the actual model
				// Note: This will test the endpoint but may fail due to size - that's expected
				logger.LogInformation("Testing download-model-range endpoint with actual model URL");
				
				try
				{
					var downloadTestUrl = new Uri(httpClient.BaseAddress!, 
						$"api/proxy/download-model-range?url={Uri.EscapeDataString(modelConfig.Url)}&start=0&end=1023").ToString();
					
					logger.LogInformation("Testing download-model-range endpoint: {DownloadTestUrl}", downloadTestUrl);
					
					// Set a shorter timeout for this test
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
					var downloadTestResponse = await httpClient.GetAsync(downloadTestUrl, cts.Token);
					
					// We expect this to either succeed with partial content or fail due to timeout/size
					// The important thing is that we don't get "Only Hugging Face model downloads are allowed"
					if (downloadTestResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
					{
						var errorContent = await downloadTestResponse.Content.ReadAsStringAsync();
						if (errorContent.Contains("Only Hugging Face model downloads are allowed"))
						{
							throw new HttpRequestException($"Model download endpoint rejected HuggingFace URL: {errorContent}");
						}
						// Other bad request errors might be acceptable for this test
						logger.LogInformation("download-model-range endpoint responded with bad request (may be expected): {ErrorContent}", errorContent);
					}
					else if (downloadTestResponse.IsSuccessStatusCode || 
							 downloadTestResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
					{
						logger.LogInformation("download-model-range endpoint test successful");
					}
					else
					{
						logger.LogInformation("download-model-range endpoint responded with: {StatusCode} (may be expected for test)", 
							downloadTestResponse.StatusCode);
					}
				}
				catch (TaskCanceledException)
				{
					// Timeout is expected for large model requests
					logger.LogInformation("download-model-range endpoint test timed out (expected for large model)");
				}
				catch (HttpRequestException ex) when (ex.Message.Contains("Only Hugging Face"))
				{
					// This indicates a real configuration problem
					throw;
				}
				catch (Exception ex)
				{
					// Other exceptions during range test are not critical
					logger.LogInformation("download-model-range endpoint test had expected error: {Message}", ex.Message);
				}
				
				logger.LogInformation("Proxy endpoint tests completed successfully");
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "Proxy endpoint test failed");
				throw new NotSupportedException(
					$"Model download requires the API service to be running for CORS proxy support. " +
					$"Proxy test failed: {ex.Message}. Please ensure the PortfolioViewer.ApiService is running and accessible, " +
					$"or use WebLLM for browser-based AI.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error during proxy endpoint test");
				throw new NotSupportedException(
					$"Unexpected error during proxy endpoint test: {ex.Message}. " +
					$"Please ensure the PortfolioViewer.ApiService is running and accessible.");
			 }
		}

		/// <summary>
		/// Test if the actual model URL is accessible
		/// </summary>
		private async Task TestModelAccessibilityAsync(ModelConfig modelConfig)
		{
			try
			{
				logger.LogInformation("Testing model accessibility: {ModelUrl}", modelConfig.Url);
				
				// Use a quick HEAD-like request to test if the URL is accessible
				// We'll use the download endpoint with a tiny range to avoid downloading the whole file
				var headTestUrl = new Uri(httpClient.BaseAddress!, 
					$"api/proxy/download-model-range?url={Uri.EscapeDataString(modelConfig.Url)}&start=0&end=0").ToString();
				
				logger.LogInformation("Testing model accessibility via range request: {TestUrl}", headTestUrl);
				
				// This should test if the model is accessible without downloading much data
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
				var headResponse = await httpClient.GetAsync(headTestUrl, cts.Token);
				
				// We expect this to potentially fail due to size, but not due to 404 or access issues
				if (headResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					throw new HttpRequestException($"Model not found at URL: {modelConfig.Url}");
				}
				
				if (headResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
				{
					throw new HttpRequestException($"Access denied to model URL: {modelConfig.Url}");
				}
				
				if (headResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
				{
					var errorContent = await headResponse.Content.ReadAsStringAsync();
					if (errorContent.Contains("Only Hugging Face model downloads are allowed"))
					{
						throw new HttpRequestException($"Proxy rejected HuggingFace URL - configuration issue: {errorContent}");
					}
					// Other bad request errors might be acceptable
					logger.LogInformation("Model accessibility test got bad request (may be expected): {ErrorContent}", errorContent);
				}
				else if (headResponse.IsSuccessStatusCode || 
						 headResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
				{
					logger.LogInformation("Model URL accessibility test successful");
				}
				else
				{
					logger.LogInformation("Model URL accessibility test got status {StatusCode} (may be expected)", headResponse.StatusCode);
				}
				
				logger.LogInformation("Model URL appears to be accessible");
			}
			catch (TaskCanceledException)
			{
				// Timeout is acceptable for this test
				logger.LogInformation("Model URL accessibility test timed out (expected for large model)");
			}
			catch (HttpRequestException ex) when (ex.Message.Contains("Model not found") || 
												   ex.Message.Contains("Access denied") || 
												   ex.Message.Contains("configuration issue"))
			{
				throw; // Re-throw these specific errors
			}
			catch (Exception ex)
			{
				// For other errors (like timeout due to large file), we'll assume the URL is accessible
				logger.LogInformation("Model URL test completed with expected error (likely due to file size): {Message}", ex.Message);
			}
		}

		/// <summary>
		/// Download model using chunked approach to avoid browser size limitations
		/// </summary>
		private async Task<string> DownloadModelInChunksAsync(IProgress<InitializeProgress>? progress, ModelConfig modelConfig)
		{
			const int MAX_RETRIES = AIModelConstants.Storage.MaxRetries;
			
			progress?.Report(new InitializeProgress(0.25, "Starting chunked download (100MB chunks)..."));
			logger.LogInformation("Starting chunked download for model with storage key: {StorageKey}", modelConfig.BrowserStorageKey);

			 // Ensure browser storage is ready before initializing download
			await EnsureBrowserStorageInitializedAsync();

			// Initialize the storage for chunked download
			try
			{
				await jsRuntime!.InvokeVoidAsync("blazorBrowserStorage.initializeModelDownload", modelConfig.BrowserStorageKey, modelConfig.SizeBytes);
				logger.LogInformation("Initialized chunked download storage for key: {StorageKey}, size: {Size}MB", 
					modelConfig.BrowserStorageKey, modelConfig.SizeBytes / 1_000_000);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to initialize model download storage with key '{StorageKey}'", modelConfig.BrowserStorageKey);
				throw new InvalidOperationException($"Could not initialize browser storage for model download: {ex.Message}");
			}

			long totalBytesDownloaded = 0;
			long currentOffset = 0;
			int chunkIndex = 0;

			try
			{
				while (currentOffset < modelConfig.SizeBytes)
				{
					var endOffset = Math.Min(currentOffset + AIModelConstants.Storage.DefaultChunkSize - 1, modelConfig.SizeBytes - 1);
					var chunkSize = endOffset - currentOffset + 1;
					
					logger.LogInformation("Downloading chunk {ChunkIndex}: bytes {Start}-{End} ({Size}MB)", 
						chunkIndex, currentOffset, endOffset, chunkSize / 1_000_000);

					// Download this chunk with retries
					byte[] chunkData = null!;
					for (int retry = 0; retry < MAX_RETRIES; retry++)
					{
						try
						{
							chunkData = await DownloadChunkAsync(currentOffset, endOffset, modelConfig);
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

					// Store chunk in IndexedDB with error handling
					try
					{
						await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.appendModelChunk", modelConfig.BrowserStorageKey, chunkData);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to store chunk {ChunkIndex} in browser storage", chunkIndex);
						throw new InvalidOperationException($"Could not store chunk {chunkIndex} in browser storage: {ex.Message}");
					}
					
					totalBytesDownloaded += chunkData.Length;
					currentOffset = endOffset + 1;
					chunkIndex++;

					// Update progress
					var progressPercent = 0.25 + (double)totalBytesDownloaded / modelConfig.SizeBytes * 0.65;
					var progressMessage = $"Downloaded {totalBytesDownloaded / 1_000_000}MB / {modelConfig.SizeBytes / 1_000_000}MB ({chunkIndex} chunks)";
					progress?.Report(new InitializeProgress(progressPercent, progressMessage));

					// Small delay to prevent overwhelming the browser
					await Task.Delay(100);
				}

				// Finalize the storage
				try
				{
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.finalizeModelDownload", modelConfig.BrowserStorageKey);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to finalize model download in browser storage");
					throw new InvalidOperationException($"Could not finalize model download: {ex.Message}");
				}

				logger.LogInformation("Successfully downloaded Phi-3 Mini model in {ChunkCount} chunks, total size: {Size}MB", 
					chunkIndex, totalBytesDownloaded / 1_000_000);
				
				progress?.Report(new InitializeProgress(0.95, "Mounting model in virtual file system..."));

				logger.LogInformation("Mounting downloaded model: StorageKey='{StorageKey}', VirtualPath='{VirtualPath}', Filename='{Filename}'", 
					modelConfig.BrowserStorageKey, modelConfig.VirtualPath, modelConfig.Filename);
				
				try
				{
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.mountModel", modelConfig.BrowserStorageKey, modelConfig.VirtualPath);
					logger.LogInformation("Successfully mounted model from storage key '{StorageKey}' to path '{ModelPath}'", 
						modelConfig.BrowserStorageKey, modelConfig.VirtualPath);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to mount downloaded model from storage key '{StorageKey}'", modelConfig.BrowserStorageKey);
					throw new InvalidOperationException($"Could not mount downloaded model: {ex.Message}");
				}
				
				progress?.Report(new InitializeProgress(1.0, "Chunked model download completed successfully"));
				return modelConfig.VirtualPath;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Chunked model download failed at chunk {ChunkIndex}", chunkIndex);
				progress?.Report(new InitializeProgress(0.0, 
					$"Chunked download failed at chunk {chunkIndex}. Using WebLLM instead."));
				
				// Clean up partial download
				try
				{
					await jsRuntime.InvokeVoidAsync("blazorBrowserStorage.deleteModel", modelConfig.BrowserStorageKey);
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
		private async Task<byte[]> DownloadChunkAsync(long startOffset, long endOffset, ModelConfig modelConfig)
		{
			// Create proxy URL with range parameters
			var rangeProxyUrl = new Uri(httpClient.BaseAddress!, 
				$"api/proxy/download-model-range?url={Uri.EscapeDataString(modelConfig.Url)}&start={startOffset}&end={endOffset}");

			logger.LogInformation("Requesting chunk: {Url}", rangeProxyUrl);

			try
			{
				using var response = await httpClient.GetAsync(rangeProxyUrl);
				
				logger.LogInformation("Chunk response: Status={StatusCode}, Length={ContentLength}", 
					response.StatusCode, response.Content.Headers.ContentLength);

				// Check for specific error responses
				if (response.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					logger.LogError("Server rejected range request with 406 Not Acceptable: {ErrorContent}", errorContent);
					throw new InvalidOperationException($"Server rejected range request: {errorContent}");
				}

				if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					logger.LogError("Bad request for range download: {ErrorContent}", errorContent);
					throw new InvalidOperationException($"Bad request for range download: {errorContent}");
				}

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					logger.LogError("Range request failed: Status={StatusCode}, Content={ErrorContent}", 
						response.StatusCode, errorContent);
					throw new HttpRequestException($"Range request failed with status {response.StatusCode}: {errorContent}");
				}

				// Verify we got the expected range response
				if (response.StatusCode != System.Net.HttpStatusCode.PartialContent && 
					response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					logger.LogWarning("Unexpected response status for range request: {StatusCode} (expected 206 or 200)", 
						response.StatusCode);
				}

				var chunkData = await response.Content.ReadAsByteArrayAsync();
				
				// Verify chunk size
				var expectedSize = endOffset - startOffset + 1;
				if (chunkData.Length != expectedSize)
				{
					logger.LogWarning("Chunk size mismatch: expected {Expected}, got {Actual}", expectedSize, chunkData.Length);
					
					// For the last chunk, it might be smaller than expected
					if (chunkData.Length == 0)
					{
						throw new InvalidOperationException($"Received empty chunk for range {startOffset}-{endOffset}");
					}
				}

				logger.LogDebug("Successfully downloaded chunk: {ActualSize} bytes for range {Start}-{End}", 
					chunkData.Length, startOffset, endOffset);

				return chunkData;
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				logger.LogError(ex, "Timeout downloading chunk {Start}-{End}", startOffset, endOffset);
				throw new HttpRequestException($"Timeout downloading chunk {startOffset}-{endOffset}", ex);
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "HTTP error downloading chunk {Start}-{End}: {Message}", startOffset, endOffset, ex.Message);
				throw;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error downloading chunk {Start}-{End}", startOffset, endOffset);
				throw new InvalidOperationException($"Failed to download chunk {startOffset}-{endOffset}: {ex.Message}", ex);
			}
		}

		/// <summary>
		/// Download model on server/desktop using traditional file system
		/// </summary>
		private async Task<string> EnsureModelDownloadedServerAsync(string targetPath, IProgress<InitializeProgress>? progress = null)
		{
			 // Get model configuration
			var modelConfig = AIModelConstants.GetModelConfig(AIModelConstants.Models.Phi3Mini4K.Filename) 
				?? throw new InvalidOperationException("Phi-3 Mini model configuration not found");
			
			// Ensure target directory exists
			Directory.CreateDirectory(targetPath);
			
			var modelFilePath = Path.Combine(targetPath, modelConfig.Filename);
			
			// Check if model already exists and has reasonable size
			if (File.Exists(modelFilePath))
			{
				var fileInfo = new FileInfo(modelFilePath);
				if (fileInfo.Length > modelConfig.SizeBytes * 0.9) // At least 90% of expected size
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

			logger.LogInformation("Downloading Phi-3 Mini model from {Url} to {Path}", modelConfig.Url, modelFilePath);
			progress?.Report(new InitializeProgress(0.1, "Starting model download..."));

			// Download directly (works only on server-side)
			using var response = await httpClient.GetAsync(modelConfig.Url, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();

			var totalBytes = response.Content.Headers.ContentLength ?? modelConfig.SizeBytes;
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
			return AIModelConstants.GetDefaultModelPaths(modelsDirectory);
		}

		/// <summary>
		/// Checks if Phi-3 Mini model exists and is valid
		/// </summary>
		/// <param name="modelPath">Path to check</param>
		/// <returns>True if model exists and appears valid</returns>
		public static bool IsModelAvailable(string modelPath)
		{
			return AIModelConstants.IsModelAvailable(modelPath);
		}
	}
}