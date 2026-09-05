using System.Text.Json;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// Persists Scalable Capital CLI API login state in %LOCALAPPDATA% so the device-code login only happens once.
	/// </summary>
	public class CliTokenStore
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

		private readonly string _filePath;

		public CliTokenStore()
		{
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhostfolioSidekick");
			Directory.CreateDirectory(dir);
			_filePath = Path.Combine(dir, "cli-tokens.json");
		}

		internal string FilePath => _filePath;

		public CliAuthSession? Load()
		{
			if (!File.Exists(_filePath))
			{
				return null;
			}

			try
			{
				var json = File.ReadAllText(_filePath);
				using var document = JsonDocument.Parse(json);
				var root = document.RootElement;
				var accessToken = root.GetProperty("accessToken").GetString();
				if (string.IsNullOrWhiteSpace(accessToken))
				{
					return null;
				}

				long expiresAtEpoch = 0;
				if (root.TryGetProperty("expiresAtEpoch", out var exp) && exp.ValueKind == System.Text.Json.JsonValueKind.Number)
				{
					expiresAtEpoch = exp.GetInt64();
				}

				return new CliAuthSession(
					accessToken,
					root.TryGetProperty("refreshToken", out var rt) ? rt.GetString() : null,
					DateTimeOffset.FromUnixTimeSeconds(expiresAtEpoch).UtcDateTime,
					root.TryGetProperty("personId", out var pid) ? pid.GetString() ?? string.Empty : string.Empty,
					root.TryGetProperty("sessionId", out var sid) ? sid.GetString() : null);
			}
			catch (JsonException)
			{
				return null;
			}
			catch (IOException)
			{
				// Unreadable token file: treat as no stored login and fall back to interactive login.
				return null;
			}
		}

		public void Save(CliAuthSession session)
		{
			var payload = new
			{
				accessToken = session.AccessToken,
				refreshToken = session.RefreshToken,
				expiresAtEpoch = new DateTimeOffset(session.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
				personId = session.PersonId,
				sessionId = session.SessionId
			};

			File.WriteAllText(_filePath, JsonSerializer.Serialize(payload, JsonOptions));
		}

		public void Clear()
		{
			if (File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
		}
	}
}
