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
		private const int MaxPromptLength = 4096; // TODO : Adjust based on model limits

		[KernelFunction("multi_step_research")]
		[Description("Perform multi-step research on a topic by making multiple queries and synthesizing the results")]
		public async Task<string> MultiStepResearch(
			[Description("The topic to research")] string topic,
			[Description("Specific aspects of the topic to research. Should be in natural language")] string[] aspects)
		{
			agentLogger.StartFunction(nameof(MultiStepResearch));
			var aspectSummaries = new List<string>();
			foreach (var aspect in aspects)
			{
				var query = $"{topic} - {aspect}";
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Searching for: {query}");
				var searchResult = await searchService.SearchAsync(query);
				var perResultSummaries = new List<string>();
				int i = 0;
				foreach (var result in searchResult.Take(3))
				{
					agentLogger.StartFunction($"{nameof(MultiStepResearch)} Summerizing search result {aspect} {++i}");
					var sanitizedContent = SanitizeText(result.Content ?? string.Empty);
					var synthesisPrompt = TruncatePrompt($"Synthesize the following research result into a concise summary. {sanitizedContent}");
					var chatResult = await chatService.GetChatMessageContentsAsync(synthesisPrompt);
					perResultSummaries.Add(string.Join(Environment.NewLine, chatResult.Select(x => x.Content)));
				}

				// Synthesize aspect summary from per-result summaries
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing aspect summary for: {aspect}");

				var aspectSynthesisPrompt = TruncatePrompt($"Synthesize the following summaries for aspect '{aspect}' into a concise aspect summary.\n{string.Join(Environment.NewLine, perResultSummaries)}");
				var aspectChatResult = await chatService.GetChatMessageContentsAsync(aspectSynthesisPrompt);
				aspectSummaries.Add(string.Join(Environment.NewLine, aspectChatResult.Select(x => x.Content)));
			}

			// Synthesize the aspect summaries into a final summary
			agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing final summary");

			var finalPrompt = TruncatePrompt($"Synthesize the following aspect summaries into a concise overall summary.\n{string.Join(Environment.NewLine, aspectSummaries)}");
			var finalChatResult = await chatService.GetChatMessageContentsAsync(finalPrompt);
			return string.Join(Environment.NewLine, finalChatResult.Select(x => x.Content));
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
			return System.Net.WebUtility.HtmlDecode(text).Trim();
		}

		[GeneratedRegex("<.*?>")]
		private static partial Regex TagRegEx();

		private static string TruncatePrompt(string prompt)
		{
			if (string.IsNullOrEmpty(prompt)) return string.Empty;
			return prompt.Length > MaxPromptLength ? prompt[..MaxPromptLength] : prompt;
		}
	}
}
