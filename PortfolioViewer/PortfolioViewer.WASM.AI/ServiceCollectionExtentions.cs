using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILogger<WebLLMChatClient>>(),
				new Dictionary<ChatMode, string> { 
					{ ChatMode.Chat, "Qwen3-4B-q4f16_1-MLC" },
					{ ChatMode.ChatWithThinking, "Qwen3-4B-q4f16_1-MLC" },
					{ ChatMode.FunctionCalling, "Hermes-3-Llama-3.1-8B-q4f16_1-MLC" },
				}
			));

			services.AddSingleton<AgentOrchestrator>();
		}
	}
}
