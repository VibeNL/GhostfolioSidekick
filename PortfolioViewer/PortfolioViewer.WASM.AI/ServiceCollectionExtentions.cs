using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		public static void AddWebChatClient(this IServiceCollection services)
		{
			//services.AddTransient<IWebChatClient>((s) => new DummyChatClient());
			
			services.AddSingleton<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				"Phi-3-mini-4k-instruct-q4f16_1-MLC"));

			services.AddTransient<IAgent, PortfolioSummaryAgent>();
			services.AddSingleton<AgentOrchestrator>((s) =>
			{
				var chatClient = s.GetRequiredService<IWebChatClient>();
				var agents = s.GetServices<IAgent>().ToList();
				return new AgentOrchestrator(chatClient, agents);
			});
		}
	}
}
