using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace PortfolioViewer.WASM.AI.Agents
{
	public static class ServiceCollectionExtentions
	{
		public static void AddMultiAgent(this IServiceCollection services)
		{
			services.AddScoped<IAgentCoordinator, AgentCoordinator>();
			services.AddScoped<GenericQueryAgent>();
			services.AddScoped<IAgent, PortfolioAgent>();
		}
	}
}
