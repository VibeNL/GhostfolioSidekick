using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		private const string modelid = "Qwen3-4B-q4f32_1-MLC";

		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<ICustomChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILogger<WebLLMChatClient>>(),
				new Dictionary<ChatMode, string> {
					{ ChatMode.Chat, modelid },
					{ ChatMode.ChatWithThinking, modelid },
					{ ChatMode.FunctionCalling, modelid },
				}
			));	
		}

		public static void AddApiChatClient(this IServiceCollection services)
		{
			services.AddSingleton<ICustomChatClient>(s => new ApiChatClient(
				s.GetRequiredService<HttpClient>(),
				s.GetRequiredService<ILogger<ApiChatClient>>()
			));
		}
	}
}



