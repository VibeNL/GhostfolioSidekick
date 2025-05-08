using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public interface IAgent
	{
		string Name { get; }
		object Description { get; }

		IAsyncEnumerable<ChatResponseUpdate> RespondAsync(IEnumerable<ChatMessage> messages, AgentContext context);
	}
}
