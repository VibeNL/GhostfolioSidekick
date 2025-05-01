using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		public static void AddWebChatClient(this IServiceCollection services)
		{
			// Register multiple WebLLMChatClient instances with different agent identifiers
			services.AddTransient<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				"Phi-3-mini-4k-instruct-q4f16_1-MLC",
				"agent1"));

			services.AddTransient<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				"Phi-3-mini-4k-instruct-q4f16_1-MLC",
				"agent2"));

			services.AddTransient<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				"Phi-3-mini-4k-instruct-q4f16_1-MLC",
				"agent3"));
		}
	}
}
