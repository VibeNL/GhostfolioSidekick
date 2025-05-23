﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public static class ResearchAgent
	{
		private const string researchAgent = @"
					You are ResearchAgent AI — a smart financial assistant. 
					You may query the internet and databases. Please state the desired query and prompt";
		public static ChatCompletionAgent Create(IWebChatClient webChatClient)
		{
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var functionCallingkernel = functionCallingBuilder.Build();

			return new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Instructions = researchAgent,
				InstructionsRole = AuthorRole.System,
				Kernel = functionCallingkernel,
				Description = "A researcher that can acces real-time data on the internet. Also can query recent financial news."
			};
		}
	}
}
