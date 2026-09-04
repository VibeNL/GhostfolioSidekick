using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	/// <summary>
	/// Provides OAuth access tokens for the Scalable Capital MCP server.
	/// On first use (or when the stored refresh token is invalid) it runs an interactive login:
	/// dynamic client registration, PKCE authorization-code flow with a localhost callback and the default browser.
	/// Tokens are persisted in %LOCALAPPDATA% so the login only happens once.
	/// </summary>
	public class McpTokenProvider : IDisposable
	{
		private static readonly Uri TokenEndpoint = new("https://mcp.scalable.capital/token");
		private static readonly Uri AuthorizeEndpoint = new("https://mcp.scalable.capital/authorize");
		private static readonly Uri RegistrationEndpoint = new("https://mcp.scalable.capital/register");

		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;
		private readonly McpTokenStore _store;
		private readonly object _lock = new();
		private string? _accessToken;
		private DateTime _expiresAtUtc;

		public McpTokenProvider(HttpClient httpClient, ILogger logger)
			: this(httpClient, logger, new McpTokenStore())
		{
		}

		internal McpTokenProvider(HttpClient httpClient, ILogger logger, McpTokenStore store)
		{
			_httpClient = httpClient;
			_logger = logger;
			_store = store;
		}

		public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
		{
			lock (_lock)
			{
				if (_accessToken != null && _expiresAtUtc > DateTime.UtcNow.AddSeconds(30))
				{
					return _accessToken;
				}
			}

			var stored = _store.Load();
			if (stored is not null)
			{
				try
				{
					await RefreshAndCacheAsync(stored.ClientId, stored.RefreshToken, cancellationToken);
					return _accessToken!;
				}
				catch (McpException ex)
				{
					_logger.LogWarning(ex, "Stored MCP refresh token is no longer valid; falling back to interactive login.");
				}
			}

			await InteractiveLoginAsync(cancellationToken);
			return _accessToken!;
		}

		private async Task RefreshAndCacheAsync(string clientId, string refreshToken, CancellationToken cancellationToken)
		{
			var response = await RefreshTokenAsync(clientId, refreshToken, cancellationToken);
			SetCachedToken(response.AccessToken, response.ExpiresIn);
			SaveTokens(clientId, response.RefreshToken ?? refreshToken);
		}

		private async Task InteractiveLoginAsync(CancellationToken cancellationToken)
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var redirectUri = $"http://localhost:{GetFreePort()}/";

			string clientId;
			try
			{
				clientId = await RegisterClientAsync(redirectUri, cts.Token);
			}
			catch (McpException ex)
			{
				throw new McpException($"Failed to register the MCP OAuth client: {ex.Message}", ex);
			}

			var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
			using var sha256 = SHA256.Create();
			var challenge = Base64UrlEncode(sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier)));
			var state = Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

			var authorizeUrl = $"{AuthorizeEndpoint}?response_type=code&client_id={Uri.EscapeDataString(clientId)}" +
				$"&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString("openid profile offline_access")}" +
				$"&state={Uri.EscapeDataString(state)}&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256";

			var callbackTask = WaitForCallbackAsync(redirectUri, state, cts.Token);
			Console.WriteLine("Opening your browser to sign in to Scalable Capital...");
			OpenBrowser(authorizeUrl);

			string code;
			try
			{
				code = await callbackTask;
			}
			catch (McpException ex)
			{
				throw new McpException($"Interactive MCP login failed: {ex.Message}", ex);
			}

			var tokenResponse = await ExchangeCodeAsync(clientId, redirectUri, verifier, code, cts.Token);
			SetCachedToken(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
			SaveTokens(clientId, tokenResponse.RefreshToken ?? string.Empty);
			_logger.LogInformation("MCP login completed; tokens stored for future runs.");
		}

		private async Task<string> RegisterClientAsync(string redirectUri, CancellationToken cancellationToken)
		{
			var body = new JsonObject
			{
				["client_name"] = "GhostfolioSidekick Scraper",
				["redirect_uris"] = new JsonArray(redirectUri),
				["grant_types"] = new JsonArray("authorization_code", "refresh_token"),
				["response_types"] = new JsonArray("code")
			};

			using var requestMessage = new HttpRequestMessage(HttpMethod.Post, RegistrationEndpoint)
			{
				Content = new StringContent(body.ToJsonString(), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
			};
			using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				throw new McpException($"HTTP {(int)response.StatusCode}: {responseBody}");
			}

			JsonNode? node;
			try
			{
				node = JsonNode.Parse(responseBody);
			}
			catch (JsonException ex)
			{
				throw new McpException($"Client registration returned malformed JSON: {responseBody}", ex);
			}

			var clientId = node?["client_id"]?.ToString();
			return string.IsNullOrWhiteSpace(clientId)
				? throw new McpException("Client registration returned no client id.")
				: clientId;
		}

		private async Task<string> WaitForCallbackAsync(string redirectUri, string expectedState, CancellationToken cancellationToken)
		{
			var listener = new HttpListener();
			listener.Prefixes.Add(redirectUri);
			listener.Start();
			try
			{
				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeoutCts.CancelAfter(TimeSpan.FromMinutes(10));

				var contextTask = listener.GetContextAsync();
				var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));
				if (completed != contextTask)
				{
					throw new McpException("MCP login timed out waiting for the browser callback.");
				}

				var context = await contextTask;
				var query = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
				var code = query["code"];
				var state = query["state"];

				if (string.IsNullOrWhiteSpace(code) || state != expectedState)
				{
					await SendResponseAsync(context, 400, "Login failed: missing or mismatched state.");
					throw new McpException("MCP login callback was invalid (missing code or state mismatch).");
				}

				await SendResponseAsync(context, 200, "Scalable Capital MCP login complete. You can close this window and return to the console.");
				return code;
			}
			finally
			{
				listener.Stop();
			}
		}

		private static async Task SendResponseAsync(HttpListenerContext context, int statusCode, string body)
		{
			context.Response.StatusCode = statusCode;
			var buffer = Encoding.UTF8.GetBytes(body);
			await context.Response.OutputStream.WriteAsync(buffer);
			context.Response.Close();
		}

		private async Task<TokenResponse> ExchangeCodeAsync(string clientId, string redirectUri, string verifier, string code, CancellationToken cancellationToken)
		{
			var form = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "authorization_code",
				["code"] = code,
				["redirect_uri"] = redirectUri,
				["client_id"] = clientId,
				["code_verifier"] = verifier
			});

			using var response = await _httpClient.PostAsync(TokenEndpoint, form, cancellationToken);
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				throw new McpException($"Failed to exchange the MCP authorization code (HTTP {(int)response.StatusCode}): {body}");
			}

			return ParseTokenResponse(body, out _);
		}

		private async Task<TokenResponse> RefreshTokenAsync(string clientId, string refreshToken, CancellationToken cancellationToken)
		{
			var form = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken,
				["client_id"] = clientId,
				["scope"] = "offline_access"
			});

			using var response = await _httpClient.PostAsync(TokenEndpoint, form, cancellationToken);
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				throw new McpException($"Failed to refresh MCP access token (HTTP {(int)response.StatusCode}): {body}");
			}

			return ParseTokenResponse(body, out _);
		}

		private TokenResponse ParseTokenResponse(string body, out string? refreshToken)
		{
			JsonNode? node;
			try
			{
				node = JsonNode.Parse(body);
			}
			catch (JsonException ex)
			{
				throw new McpException($"MCP token endpoint returned malformed JSON: {body}", ex);
			}

			var accessToken = node?["access_token"]?.ToString();
			var expiresIn = int.TryParse(node?["expires_in"]?.ToString(), out var parsedExpiresIn) ? parsedExpiresIn : 1200;
			refreshToken = node?["refresh_token"]?.ToString();
			if (string.IsNullOrWhiteSpace(accessToken))
			{
				throw new McpException("MCP token endpoint returned no access token.");
			}

			return new TokenResponse(accessToken, expiresIn, refreshToken);
		}

		private void SetCachedToken(string accessToken, int expiresInSeconds)
		{
			lock (_lock)
			{
				_accessToken = accessToken;
				_expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds - 60);
			}
		}

		private void SaveTokens(string clientId, string refreshToken)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(refreshToken))
				{
					_store.Save(new McpTokenStore.StoredTokens { ClientId = clientId, RefreshToken = refreshToken });
				}
			}
			catch (IOException ex)
			{
				_logger.LogWarning(ex, "Could not persist MCP tokens; an interactive login may be required on the next run.");
			}
		}

		private static int GetFreePort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}

		private static void OpenBrowser(string url)
		{
			try
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				throw new McpException($"Could not open the default browser. Open this URL manually to complete the login: {url}", ex);
			}
		}

		private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		/// <summary>Forces a token refresh on the next call (used after a 401).</summary>
		public void Invalidate()
		{
			lock (_lock)
			{
				_accessToken = null;
				_expiresAtUtc = DateTime.MinValue;
			}
		}

		public void Dispose()
		{
		}

		private record TokenResponse(string AccessToken, int ExpiresIn, string? RefreshToken);
	}
}
