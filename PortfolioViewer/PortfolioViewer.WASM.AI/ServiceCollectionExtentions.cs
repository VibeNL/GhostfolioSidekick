using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<IWebChatClient>(new SimpleWebLLMChatClient("phi-3-mini-4k-instruct-q4f16_1-MLC"));
		}
	}
}
