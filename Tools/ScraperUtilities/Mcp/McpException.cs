namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	public class McpException : Exception
	{
		public McpException(string message) : base(message)
		{
		}

		public McpException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
