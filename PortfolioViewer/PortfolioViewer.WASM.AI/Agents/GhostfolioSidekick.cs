using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
    public static class GhostfolioSidekick
    {
        private static string BuildPromptWithCompanions(IEnumerable<Agent> companions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"The current UTC date is {DateTime.UtcNow:yyyy-MM-dd}.");
            sb.AppendLine("You are GhostfolioSidekick — a smart AI assistant for helping users understand and manage their investment portfolio.");
            sb.AppendLine("Respond clearly, use markdown if helpful, and think in terms of financial reasoning and portfolio analysis.");
            sb.AppendLine();
            sb.AppendLine("You can delegate specific tasks to companion agents. When a user asks something that should be handled by a companion:");
            sb.AppendLine("- Do not explain anything yourself.");
            sb.AppendLine("- Do not respond with markdown.");
            sb.AppendLine("- Simply respond with a single JSON block as shown below.");
            sb.AppendLine();
            sb.AppendLine("Respond ONLY with this format when calling a companion agent:");
            sb.AppendLine("{");
            sb.AppendLine("  \"agent\": \"AgentName\",");
            sb.AppendLine("  \"input\": \"The input or question you want to pass to that agent.\"");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("User: What does ISIN IE00B4L5Y983 mean?");
            sb.AppendLine("Assistant:");
            sb.AppendLine("{");
            sb.AppendLine("  \"agent\": \"GetAssetInfo\",");
            sb.AppendLine("  \"input\": \"IE00B4L5Y983\"");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Here are the available companions:");
            foreach (var companion in companions)
            {
                sb.AppendLine($"- {companion.Name}: {companion.Description}");
            }
            return sb.ToString();
        }

        public static ChatCompletionAgent Create(IWebChatClient webChatClient, IEnumerable<Agent> companions)
        {
            IKernelBuilder thinkBuilder = Kernel.CreateBuilder();
            thinkBuilder.Services.AddScoped<IChatCompletionService>((s) =>
            {
                var client = webChatClient.Clone();
                client.ChatMode = ChatMode.ChatWithThinking;
                return client.AsChatCompletionService();
            });
            var thinkingKernel = thinkBuilder.Build();

            return new ChatCompletionAgent
            {
                Name = "GhostfolioSidekick",
                Instructions = BuildPromptWithCompanions(companions),
                Kernel = thinkingKernel,
                Description = "A smart financial assistant that helps users understand and manage their investment portfolio.",
                InstructionsRole = AuthorRole.System
            };
        }
    }
}