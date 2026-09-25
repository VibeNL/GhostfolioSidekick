using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace GhostfolioSidekick.AI.Agents
{
	public static class ServiceCollectionExtentions
	{
		public static void AddAgents(this IServiceCollection services)
		{
			services.AddSingleton<AgentLogger>();
			services.AddSingleton<AgentOrchestrator>();

			// Typed client gives us a resolvable GoogleSearchService + HttpClient; standard resilience adds retries, timeout and circuit breaker.
			services.AddHttpClient<GoogleSearchService>().AddStandardResilienceHandler();
			services.AddSingleton<IGoogleSearchService>(s => s.GetRequiredService<GoogleSearchService>());

			services.AddSingleton<IAgentToolProvider, ResearchAgentToolProvider>();
		}
	}
}
