using System.Text.Json.Nodes;

namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	/// <summary>
	/// Stateless JSON-RPC tool-call surface of the Scalable Capital MCP server.
	/// </summary>
	public interface IMcpClient : IDisposable
	{
		Task<JsonObject> CallToolAsync(string toolName, JsonObject arguments, CancellationToken cancellationToken);
	}
}
