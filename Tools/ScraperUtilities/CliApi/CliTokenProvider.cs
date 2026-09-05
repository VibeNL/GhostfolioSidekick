using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// Provides authenticated sessions for the Scalable Capital CLI API (same flow as the official scalable-cli):
	/// device-code login on first use, refresh-token rotation afterwards. Tokens are persisted in %LOCALAPPDATA%.
	/// </summary>
	public class CliTokenProvider : IDisposable
	{
		private const string ClientId = "yBM3BrpRgwSTJZRdJllvtD6jJEmyxWfE";
		private const string Audience = "https://de.scalable.capital/api-gateway";
		private const string Scope = "offline_access openid email";

		private static readonly Uri Issuer = new("https://secure.scalable.capital");

		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;
		private readonly CliTokenStore _store;
		private readonly DpopKey _dpopKey;
		private readonly object _lock = new();
		private CliAuthSession? _session;

		public DpopKey Key => _dpopKey;

		public CliTokenProvider(HttpClient httpClient, ILogger logger)
			: this(httpClient, logger, new CliTokenStore(), LoadOrCreateDpopKey())
		{
		}

		internal CliTokenProvider(HttpClient httpClient, ILogger logger, CliTokenStore store, DpopKey dpopKey)
		{
			_httpClient = httpClient;
			_logger = logger;
			_store = store;
			_dpopKey = dpopKey;
		}

		public async Task<CliAuthSession> GetSessionAsync(CancellationToken cancellationToken)
		{
			lock (_lock)
			{
				if (_session != null && _session.ExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
				{
					return _session;
				}
			}

			var stored = _store.Load();
			if (stored is not null)
			{
				try
				{
					await RefreshAsync(stored, cancellationToken);
					return _session!;
				}
				catch (CliApiException ex)
				{
					_logger.LogWarning(ex, "Stored CLI API refresh token is no longer valid; falling back to device-code login.");
				}
			}

			await DeviceCodeLoginAsync(cancellationToken);
			return _session!;
		}

		private async Task RefreshAsync(CliAuthSession stored, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(stored.RefreshToken))
			{
				throw new CliApiException("Stored CLI API session has no refresh token.");
			}

			var form = new Dictionary<string, string>
			{
				["grant_type"] = "refresh_token",
				["client_id"] = ClientId,
				["refresh_token"] = stored.RefreshToken
			};

			if (!string.IsNullOrWhiteSpace(stored.SessionId))
			{
				form["session_id"] = stored.SessionId;
			}

			var body = await PostFormAsync($"{Issuer}/oauth/token", form, null, cancellationToken);
			await ApplyTokenResponseAsync(body, stored.PersonId, cancellationToken);
		}

		private async Task DeviceCodeLoginAsync(CancellationToken cancellationToken)
		{
			var startBody = await PostFormAsync($"{Issuer}/oauth/device/code", new Dictionary<string, string>
			{
				["client_id"] = ClientId,
				["audience"] = Audience,
				["scope"] = Scope
			}, null, cancellationToken);

			using var startDocument = JsonDocument.Parse(startBody);
			var deviceCode = RequireString(startDocument.RootElement, "device_code");
			var userCode = RequireString(startDocument.RootElement, "user_code");
			var verificationUri = startDocument.RootElement.TryGetProperty("verification_uri_complete", out var complete) && !string.IsNullOrWhiteSpace(complete.GetString())
				? complete.GetString()!
				: RequireString(startDocument.RootElement, "verification_uri");
			var expiresIn = startDocument.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 600;
			var intervalSeconds = Math.Max(1, startDocument.RootElement.TryGetProperty("interval", out var interval) && interval.ValueKind == JsonValueKind.Number ? interval.GetInt32() : 5);

			Console.WriteLine("Sign in to Scalable Capital:");
			Console.WriteLine($"Open this URL: {verificationUri}");
			Console.WriteLine($"Verify the code {userCode} in your browser.");
			OpenBrowser(verificationUri);

			var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (DateTime.UtcNow >= expiresAt)
				{
					throw new CliApiException("Device code login expired before completion.");
				}

				await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);

				var form = new Dictionary<string, string>
				{
					["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
					["device_code"] = deviceCode,
					["client_id"] = ClientId
				};

				try
				{
					var body = await PostFormAsync($"{Issuer}/oauth/token", form, null, cancellationToken);
					await ApplyTokenResponseAsync(body, string.Empty, cancellationToken);
					return;
				}
				catch (CliApiException ex) when (ex.StatusCode == 400 && !string.IsNullOrWhiteSpace(ex.ResponseBody))
				{
					var state = ParseOAuthError(ex.ResponseBody!);
					if (state == "authorization_pending")
					{
						continue;
					}

					if (state == "slow_down")
					{
						intervalSeconds += 2;
						Console.WriteLine($"Waiting for browser confirmation... (polling every {intervalSeconds}s)");
						continue;
					}

					throw new CliApiException(state switch
					{
						"access_denied" => "Device login denied by user.",
						"expired_token" => "Device login code expired.",
						_ => $"Device code polling failed: {state}"
					});
				}
			}
		}

		private async Task ApplyTokenResponseAsync(string body, string fallbackPersonId, CancellationToken cancellationToken)
		{
			using var document = JsonDocument.Parse(body);
			var root = document.RootElement;
			var accessToken = RequireString(root, "access_token");
			var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				throw new CliApiException("CLI API token endpoint returned no refresh token.");
			}

			var expiresAtUtc = DateTime.UtcNow.AddSeconds(root.TryGetProperty("expires_in", out var exp) && exp.ValueKind == JsonValueKind.Number ? exp.GetInt32() : 1200);
			var claims = DecodeJwtClaims(accessToken);
			var personId = FirstClaim(claims, "person_id", "https://de.scalable.capital/person_id", "https://de.scalable.capital/personId") ?? fallbackPersonId;
			if (string.IsNullOrWhiteSpace(personId))
			{
				throw new CliApiException("CLI API access token does not contain a person_id claim.");
			}

			var sessionId = FirstClaim(claims, "session_id", "https://de.scalable.capital/session_id");
			lock (_lock)
			{
				_session = new CliAuthSession(accessToken, refreshToken, expiresAtUtc, personId, sessionId);
			}

			try
			{
				_store.Save(_session!);
			}
			catch (IOException ex)
			{
				_logger.LogWarning(ex, "Could not persist CLI API tokens; a login may be required on the next run.");
			}
		}

		private async Task<string> PostFormAsync(string url, Dictionary<string, string> form, string? accessToken, CancellationToken cancellationToken)
		{
			var uri = new Uri(url);
			string? nonce = null;
			for (var attempt = 0; attempt < 2; attempt++)
			{
				using var content = new FormUrlEncodedContent(form);
				using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
				request.Headers.Add("DPoP", _dpopKey.BuildProof("POST", uri, nonce, accessToken));

				var response = await _httpClient.SendAsync(request, cancellationToken);
				string responseBody;
				try
				{
					responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						return responseBody;
					}

					var retryNonce = GetDpopNonce(response.Headers);
					if (attempt == 0 && ShouldRetryWithDpopNonce((int)response.StatusCode, retryNonce, responseBody))
					{
						nonce = retryNonce;
						continue;
					}

					throw new CliApiException($"CLI API HTTP error {(int)response.StatusCode}: {responseBody}", (int)response.StatusCode, responseBody);
				}
				finally
				{
					response.Dispose();
				}
			}

			throw new InvalidOperationException("Unreachable DPoP retry loop exit.");
		}

		private static string? GetDpopNonce(HttpResponseHeaders headers) =>
			headers.TryGetValues("DPoP-Nonce", out var values) ? values.FirstOrDefault() : null;

		private static bool ShouldRetryWithDpopNonce(int statusCode, string? nonce, string body)
		{
			if (string.IsNullOrWhiteSpace(nonce))
			{
				return false;
			}

			var lower = body.ToLowerInvariant();
			return statusCode == 401 || lower.Contains("use_dpop_nonce") || lower.Contains("invalid_dpop_proof");
		}

		private static string? ParseOAuthError(string body)
		{
			try
			{
				using var document = JsonDocument.Parse(body);
				return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static Dictionary<string, JsonElement> DecodeJwtClaims(string jwt)
		{
			var parts = jwt.Split('.');
			if (parts.Length < 2)
			{
				throw new CliApiException("CLI API access token is not a JWT.");
			}

			var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
			using var document = JsonDocument.Parse(payload);
			var claims = new Dictionary<string, JsonElement>();
			foreach (var property in document.RootElement.EnumerateObject())
			{
				claims[property.Name] = property.Value.Clone();
			}

			return claims;
		}

		private static string? FirstClaim(Dictionary<string, JsonElement> claims, params string[] names)
		{
			foreach (var name in names)
			{
				if (claims.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String)
				{
					return value.GetString();
				}
			}

			return null;
		}

		private static string RequireString(JsonElement element, string name) =>
			element.TryGetProperty(name, out var property) ? property.GetString() ?? throw new CliApiException($"CLI API response is missing '{name}'.") : throw new CliApiException($"CLI API response is missing '{name}'.");

		private static DpopKey LoadOrCreateDpopKey()
		{
			var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhostfolioSidekick");
			Directory.CreateDirectory(dir);
			var path = Path.Combine(dir, "cli-auth-signing-key.json");
			try
			{
				if (File.Exists(path))
				{
					return DpopKey.Load(File.ReadAllText(path));
				}

				var key = DpopKey.Create();
				File.WriteAllText(path, key.ToJson());
				return key;
			}
			catch (IOException)
			{
				return DpopKey.Create();
			}
		}

		private static void OpenBrowser(string url)
		{
			try
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				throw new CliApiException($"Could not open the default browser. Open this URL manually to complete the login: {url}", ex);
			}
		}

		private static byte[] Base64UrlDecode(string value)
		{
			var base64 = value.Replace('-', '+').Replace('_', '/');
			return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
		}

		public void Dispose() => _dpopKey.Dispose();
	}

	public record CliAuthSession(string AccessToken, string? RefreshToken, DateTime ExpiresAtUtc, string PersonId, string? SessionId);

	public class CliApiException : Exception
	{
		public int? StatusCode { get; }
		public string? ResponseBody { get; }

		public CliApiException(string message)
			: base(message)
		{
		}

		public CliApiException(string message, int statusCode, string responseBody)
			: base(message)
		{
			StatusCode = statusCode;
			ResponseBody = responseBody;
		}

		public CliApiException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}