using System.Text.Json.Nodes;
using GhostfolioSidekick.Tools.ScraperUtilities.Mcp;

namespace GhostfolioSidekick.Tools.ScraperUtilities.UnitTests
{
	/// <summary>
	/// In-memory IMcpClient that returns canned tool responses and records every call.
	/// </summary>
	public class FakeMcpClient : IMcpClient
	{
		private readonly Func<string, JsonObject?, JsonObject> _responder;

		public FakeMcpClient(Func<string, JsonObject?, JsonObject> responder)
		{
			_responder = responder;
		}

		public List<(string ToolName, JsonObject Arguments)> Calls { get; } = [];

		public Task<JsonObject> CallToolAsync(string toolName, JsonObject arguments, CancellationToken cancellationToken)
		{
			Calls.Add((toolName, arguments));
			return Task.FromResult(_responder(toolName, arguments));
		}

		public void Dispose()
		{
		}
	}
}
