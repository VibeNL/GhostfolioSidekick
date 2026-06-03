using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Common
{
	/// <summary>
	/// Implemented by any component that wants to contribute tools and companion descriptions
	/// to the main AI agent. Register implementations in DI as <see cref="IAgentToolProvider"/>.
	/// </summary>
	public interface IAgentToolProvider
	{
		/// <summary>A short human-readable name shown in the agent's system prompt.</summary>
		string ProviderName { get; }

		/// <summary>A one-sentence description of what this provider can do.</summary>
		string ProviderDescription { get; }

		/// <summary>The set of <see cref="AITool"/> instances to expose to the agent.</summary>
		IReadOnlyList<AITool> GetTools();
	}
}
