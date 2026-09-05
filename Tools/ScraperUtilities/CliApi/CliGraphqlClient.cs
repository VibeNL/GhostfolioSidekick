using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// Minimal GraphQL client for the Scalable Capital CLI API endpoint, using DPoP-protected access tokens.
	/// Mirrors the official scalable-cli retry policy: one retry with a fresh nonce on a DPoP challenge.
	/// </summary>
	public class CliGraphqlClient : IDisposable
	{
		private static readonly Uri Endpoint = new("https://de.scalable.capital/api/cli/graphql");

		private readonly HttpClient _httpClient;
		private readonly DpopKey _dpopKey;
		private readonly CliTokenProvider _tokenProvider;

		public CliGraphqlClient(HttpClient httpClient, DpopKey dpopKey, CliTokenProvider tokenProvider)
		{
			_httpClient = httpClient;
			_dpopKey = dpopKey;
			_tokenProvider = tokenProvider;
		}

		public async Task<JsonNode> QueryAsync(string query, object? variables, string operationName, CancellationToken cancellationToken)
		{
			string? nonce = null;
			for (var attempt = 0; attempt < 2; attempt++)
			{
				var body = new JsonObject
				{
					["query"] = query,
					["variables"] = variables is null ? new JsonObject() : JsonSerializer.SerializeToNode(variables)!,
					["operationName"] = operationName
				};

				using var content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
				using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
				var session = await _tokenProvider.GetSessionAsync(cancellationToken);
				request.Headers.Add("Authorization", $"DPoP {session.AccessToken}");
				request.Headers.Add("DPoP", _dpopKey.BuildProof("POST", Endpoint, nonce, session.AccessToken));

				string responseBody;
				HttpResponseMessage? response = null;
				try
				{
					response = await _httpClient.SendAsync(request, cancellationToken);
					responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						return ParseData(responseBody, operationName);
					}

					var retryNonce = GetDpopNonce(response.Headers);
					if (attempt == 0 && ShouldRetryWithDpopNonce((int)response.StatusCode, retryNonce, responseBody))
					{
						nonce = retryNonce;
						continue;
					}

					throw new CliApiException($"GraphQL HTTP error {(int)response.StatusCode} during {operationName}: {responseBody}");
				}
				finally
				{
					response?.Dispose();
				}
			}

			throw new InvalidOperationException("Unreachable DPoP retry loop exit.");
		}

		private static JsonNode ParseData(string responseBody, string operationName)
		{
			JsonNode? node;
			try
			{
				node = JsonNode.Parse(responseBody);
			}
			catch (JsonException ex)
			{
				throw new CliApiException($"GraphQL response for {operationName} is not valid JSON: {responseBody}", ex);
			}

			var errors = node?["errors"]?.AsArray();
			if (errors != null && errors.Count > 0)
			{
				var message = string.Join("; ", errors.Select(e => e?["message"]?.ToString() ?? "unknown error"));
				throw new CliApiException($"GraphQL error during {operationName}: {message}");
			}

			return node?["data"] ?? throw new CliApiException($"GraphQL response for {operationName} has no data.");
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

		public void Dispose() => _dpopKey.Dispose();
	}
}