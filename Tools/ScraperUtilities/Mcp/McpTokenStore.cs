using System.Text.Json;

namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	/// <summary>
	/// Persists MCP OAuth state (client id + refresh token) in %LOCALAPPDATA% so the interactive login only happens once.
	/// </summary>
	public class McpTokenStore
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

		private readonly string _filePath;

		public McpTokenStore()
		{
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhostfolioSidekick");
			Directory.CreateDirectory(dir);
			_filePath = Path.Combine(dir, "mcp-tokens.json");
		}

		public StoredTokens? Load()
		{
			if (!File.Exists(_filePath))
			{
				return null;
			}

			try
			{
				var tokens = JsonSerializer.Deserialize<StoredTokens>(File.ReadAllText(_filePath), JsonOptions);
				return string.IsNullOrWhiteSpace(tokens?.RefreshToken) ? null : tokens;
			}
			catch (JsonException)
			{
				return null;
			}
		}

		public void Save(StoredTokens tokens)
		{
			File.WriteAllText(_filePath, JsonSerializer.Serialize(tokens, JsonOptions));
		}

		public class StoredTokens
		{
			public string ClientId { get; set; } = string.Empty;
			public string RefreshToken { get; set; } = string.Empty;
		}
	}
}
