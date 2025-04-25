
using Microsoft.Extensions.AI;

namespace PortfolioViewer.WASM.AI.Agents
{
	public interface IAgentCoordinator
	{
		IAsyncEnumerable<ChatResponseUpdate> RunAgentsAsync(IEnumerable<ChatMessage> input);
	}
}