using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class AgentContext
	{
		public IList<ChatMessage> Memory { get; } = [];
	}
}
