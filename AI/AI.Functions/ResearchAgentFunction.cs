using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.AI.Functions
{
	public partial class ResearchAgentFunction(IGoogleSearchService searchService, IChatCompletionService chatService, AgentLogger agentLogger)
	{
		[KernelFunction("multi_step_research")]
		[Description("Perform multi-step research on a topic by making multiple queries and synthesizing the results")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task<string> MultiStepResearch(
			[Description("The topic to research")] string topic,
			[Description("Specific aspects of the topic to research. Should be in natural language")] string[] aspects)
		{
			agentLogger.StartFunction(nameof(MultiStepResearch));
			var results = new StringBuilder();
			foreach (var aspect in aspects)
			{
				var query = $"{topic} - {aspect}";
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Searching for: {query}");
				var searchResult = await searchService.SearchAsync(query);
				results.AppendLine($"Aspect: {aspect}\n{string.Join(
					Environment.NewLine, 
					searchResult.Take(3).Select(x => $"[{SanitizeText(x.Title ?? string.Empty)}] {SanitizeText(x.Content ?? string.Empty)}"))}\n");
			}

			agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing results");
			var synthesisPrompt = $"Synthesize the following research results into a concise summary.\n{string.Join(Environment.NewLine, results)}";
			var chatResult = await chatService.GetChatMessageContentsAsync(synthesisPrompt);
			return string.Join(Environment.NewLine, chatResult.Select(x => x.Content));
		}

		private static string SanitizeText(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return string.Empty;
			}

			// Remove HTML tags
			var text = TagRegEx().Replace(input, string.Empty);
			
			// Optionally, decode HTML entities
			return System.Net.WebUtility.HtmlDecode(text);
		}

		[GeneratedRegex("<.*?>")]
		private static partial Regex TagRegEx();
	}
}
