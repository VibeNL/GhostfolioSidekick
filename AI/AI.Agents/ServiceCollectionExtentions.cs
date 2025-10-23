using AI.Functions.OnlineSearch;
using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.AI.Agents
{
	public static class ServiceCollectionExtentions
	{
		public static void AddAgents(this IServiceCollection services)
		{
			services.AddSingleton<AgentLogger>();
			services.AddSingleton<AgentOrchestrator>();

			// Register Google Search service with MCP pattern
			services.AddHttpClient<GoogleSearchService>();
			services.AddSingleton<IGoogleSearchProtocol, GoogleSearchService>();
			services.AddSingleton((s) =>
			{
				var httpClient = s.GetRequiredService<HttpClient>();
				// Create a context for the GoogleSearchService
				var context = new GoogleSearchContext
				{
					HttpClient = httpClient,
					// Default URLs are already set in the context class
				};
				// Return the service with the context
				return new GoogleSearchService(context);
			});
		}
	}
}
