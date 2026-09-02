using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	/// <summary>
	/// Minimal stateless JSON-RPC 2.0 client for the Scalable Capital MCP server (streamable HTTP transport).
	/// </summary>
	public class McpClient : IDisposable
	{
		private readonly HttpClient _httpClient;
		private readonly McpTokenProvider _tokenProvider;
		private readonly ILogger _logger;
		private int _nextId = 1;

		public McpClient(HttpClient httpClient, McpTokenProvider tokenProvider, ILogger logger)
		{
			_httpClient = httpClient;
			_tokenProvider = tokenProvider;
			_logger = logger;
		}

		public async Task<JsonObject> CallToolAsync(string toolName, JsonObject arguments, CancellationToken cancellationToken)
		{
			var parameters = new JsonObject
			{
				["name"] = toolName,
				["arguments"] = arguments
			};

			var result = await SendRequestAsync("tools/call", parameters, cancellationToken);
			if (result is null)
			{
				throw new McpException($"Tool '{toolName}' returned no result.");
			}

			var content = result["content"]?.AsArray();
			var text = content?.FirstOrDefault(c => c?["type"]?.ToString() == "text")?["text"]?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new McpException($"Tool '{toolName}' returned no text content.");
			}

			JsonObject? parsed;
			try
			{
				parsed = JsonNode.Parse(text) as JsonObject;
			}
			catch (JsonException ex)
			{
				throw new McpException($"Tool '{toolName}' returned malformed JSON payload.", ex);
			}

			return parsed ?? throw new McpException($"Tool '{toolName}' returned non-object JSON payload.");
		}

		private async Task<JsonObject?> SendRequestAsync(string method, JsonObject parameters, CancellationToken cancellationToken)
		{
			var id = _nextId++;
			var request = new JsonObject
			{
				["jsonrpc"] = "2.0",
				["id"] = id,
				["method"] = method,
				["params"] = parameters
			};

			for (var attempt = 0; attempt < 2; attempt++)
			{
				var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
				using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://mcp.scalable.capital/mcp")
				{
					Content = new StringContent(request.ToJsonString(), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
				};
				requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
				requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

				using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);

				if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
				{
					_logger.LogWarning("MCP request rejected with 401; refreshing token and retrying once.");
					_tokenProvider.Invalidate();
					continue;
				}

				if (!response.IsSuccessStatusCode)
				{
					throw new McpException($"MCP request '{method}' failed (HTTP {(int)response.StatusCode}): {body}");
				}

				JsonNode? payload;
				try
				{
					payload = JsonNode.Parse(body);
				}
				catch (JsonException ex)
				{
					throw new McpException($"MCP request '{method}' returned malformed JSON response.", ex);
				}

				if (payload?["error"] is JsonObject error)
				{
					throw new McpException($"MCP request '{method}' returned JSON-RPC error: {error.ToJsonString()}");
				}

				return payload?["result"] as JsonObject;
			}

			throw new McpException("Unreachable.");
		}

		public void Dispose()
		{
		}
	}
}
