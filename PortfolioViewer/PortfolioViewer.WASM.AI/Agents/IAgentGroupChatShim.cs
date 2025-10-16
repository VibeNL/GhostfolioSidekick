namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	// Shim interface and default wrapper to allow tests to inject a fake group chat
	public interface IAgentGroupChatShim
	{
		void AddChatMessage(SimpleStreamingMessage message);
		IAsyncEnumerable<SimpleStreamingMessage> InvokeStreamingAsync(CancellationToken cancellationToken = default);
		IAsyncEnumerable<SimpleStreamingMessage> GetChatMessagesAsync();
	}
}
