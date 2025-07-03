namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	/// <summary>
	/// Centralized configuration for AI model definitions and mapping
	/// </summary>
	public static class AIModelConstants
	{
		/// <summary>
		/// Model definitions for supported AI models
		/// </summary>
		public static class Models
		{
			/// <summary>
			/// Microsoft Phi-3 Mini 4K Instruct model configuration
			/// </summary>
			public static class Phi3Mini4K
			{
				public const string Id = "phi3-mini-4k";
				public const string Name = "Phi-3 Mini 4K Instruct";
				public const string Filename = "Phi-3-mini-4k-instruct-q4.gguf";
				public const string Url = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf";
				public const long SizeBytes = 2_240_000_000; // ~2.24GB
				public const string BrowserStorageKey = "llama_model_phi3_mini";
				public const string VirtualPathTemplate = "/models/{0}"; // Use with string.Format(VirtualPathTemplate, Filename)
				
				/// <summary>
				/// Alternative filenames that should map to this model
				/// </summary>
				public static readonly string[] AlternativeFilenames = [
					"phi-3-mini-4k-instruct.q4_0.gguf",
					"phi-3-mini-4k-instruct.Q4_0.gguf",
					"phi-3-mini-4k-instruct-q4.gguf",
					"Phi-3-mini-4k-instruct-q4.gguf"
				];
				
				/// <summary>
				/// Partial name patterns that should match this model
				/// </summary>
				public static readonly string[] NamePatterns = [
					"phi-3-mini",
					"phi3-mini",
					"Phi-3-mini",
					"PHI-3-MINI"
				];
			}
		}

		/// <summary>
		/// Chat mode definitions for WebLLM models
		/// </summary>
		public static class WebLLMModels
		{
			public const string DefaultModelId = "Qwen3-4B-q4f32_1-MLC";
		}

		/// <summary>
		/// Storage and caching configuration
		/// </summary>
		public static class Storage
		{
			public const string IndexedDBName = "AIModelsDB";
			public const int IndexedDBVersion = 1;
			public const long DefaultChunkSize = 100 * 1024 * 1024; // 100MB chunks
			public const int MaxRetries = 3;
			public const string ModelKeyPrefix = "llama_model_";
		}

		/// <summary>
		/// Gets the browser storage key for a given model filename
		/// </summary>
		/// <param name="modelFilename">The model filename to check</param>
		/// <returns>The storage key to use in IndexedDB</returns>
		public static string GetStorageKeyForModel(string modelFilename)
		{
			if (string.IsNullOrWhiteSpace(modelFilename))
			{
				throw new ArgumentException("Model filename cannot be null or empty", nameof(modelFilename));
			}

			var normalizedFilename = modelFilename.ToLowerInvariant();

			// Check Phi-3 Mini 4K model
			if (IsMatchingModel(normalizedFilename, Models.Phi3Mini4K.Filename, 
								Models.Phi3Mini4K.AlternativeFilenames, 
								Models.Phi3Mini4K.NamePatterns))
			{
				return Models.Phi3Mini4K.BrowserStorageKey;
			}

			// For unknown models, use a sanitized version of the filename
			return Storage.ModelKeyPrefix + SanitizeFilename(modelFilename);
		}

		/// <summary>
		/// Gets the virtual path for a model filename in WASM environment
		/// </summary>
		/// <param name="modelFilename">The model filename</param>
		/// <returns>The virtual path for the model</returns>
		public static string GetVirtualPath(string modelFilename)
		{
			if (string.IsNullOrWhiteSpace(modelFilename))
			{
				throw new ArgumentException("Model filename cannot be null or empty", nameof(modelFilename));
			}

			return string.Format(Models.Phi3Mini4K.VirtualPathTemplate, modelFilename);
		}

		/// <summary>
		/// Gets model configuration by filename
		/// </summary>
		/// <param name="modelFilename">The model filename to check</param>
		/// <returns>Model configuration or null if not found</returns>
		public static ModelConfig? GetModelConfig(string modelFilename)
		{
			if (string.IsNullOrWhiteSpace(modelFilename))
			{
				return null;
			}

			var normalizedFilename = modelFilename.ToLowerInvariant();

			// Check Phi-3 Mini 4K model
			if (IsMatchingModel(normalizedFilename, Models.Phi3Mini4K.Filename, 
								Models.Phi3Mini4K.AlternativeFilenames, 
								Models.Phi3Mini4K.NamePatterns))
			{
				return new ModelConfig
				{
					Id = Models.Phi3Mini4K.Id,
					Name = Models.Phi3Mini4K.Name,
					Filename = Models.Phi3Mini4K.Filename,
					Url = Models.Phi3Mini4K.Url,
					SizeBytes = Models.Phi3Mini4K.SizeBytes,
					BrowserStorageKey = Models.Phi3Mini4K.BrowserStorageKey,
					VirtualPath = GetVirtualPath(Models.Phi3Mini4K.Filename)
				};
			}

			return null;
		}

		/// <summary>
		/// Gets the default model paths for LLamaSharp
		/// </summary>
		/// <param name="modelsDirectory">Base models directory (for server environments)</param>
		/// <returns>Dictionary mapping ChatMode to model paths</returns>
		public static Dictionary<ChatMode, string> GetDefaultModelPaths(string modelsDirectory = "wwwroot/models")
		{
			var isWasm = IsWasmEnvironment();
			var modelPath = isWasm 
				? GetVirtualPath(Models.Phi3Mini4K.Filename) // Virtual path in WASM
				: Path.Combine(modelsDirectory, Models.Phi3Mini4K.Filename); // Physical path on server

			return new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, modelPath },
				{ ChatMode.ChatWithThinking, modelPath },
				{ ChatMode.FunctionCalling, modelPath }
			};
		}

		/// <summary>
		/// Checks if a model file is available (server environments only)
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
				var modelConfig = GetModelConfig(Path.GetFileName(modelPath));
				
				if (modelConfig == null)
				{
					// Unknown model, assume any non-empty file is valid
					return fileInfo.Length > 0;
				}

				// For known models, check if file is at least 90% of expected size
				return fileInfo.Length > modelConfig.SizeBytes * 0.9;
			}
			catch
			{
				// File system access failed
				return false;
			}
		}

		/// <summary>
		/// Helper method to check if a filename matches a model
		/// </summary>
		private static bool IsMatchingModel(string normalizedFilename, string primaryFilename, 
										   string[] alternativeFilenames, string[] namePatterns)
		{
			// Check exact match with primary filename
			if (normalizedFilename == primaryFilename.ToLowerInvariant())
			{
				return true;
			}

			// Check alternative filenames
			foreach (var altFilename in alternativeFilenames)
			{
				if (normalizedFilename == altFilename.ToLowerInvariant())
				{
					return true;
				}
			}

			// Check name patterns
			foreach (var pattern in namePatterns)
			{
				if (normalizedFilename.Contains(pattern.ToLowerInvariant()))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Sanitizes a filename for use as a storage key
		/// </summary>
		private static string SanitizeFilename(string filename)
		{
			return filename.Replace('.', '_').Replace('-', '_').ToLowerInvariant();
		}

		/// <summary>
		/// Detects if running in WebAssembly environment
		/// </summary>
		private static bool IsWasmEnvironment()
		{
			return System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
				   Environment.OSVersion.Platform == PlatformID.Other ||
				   Type.GetType("System.Runtime.InteropServices.JavaScript.JSHost") != null;
		}
	}

	/// <summary>
	/// Configuration data for an AI model
	/// </summary>
	public class ModelConfig
	{
		public required string Id { get; init; }
		public required string Name { get; init; }
		public required string Filename { get; init; }
		public required string Url { get; init; }
		public required long SizeBytes { get; init; }
		public required string BrowserStorageKey { get; init; }
		public required string VirtualPath { get; init; }
	}
}