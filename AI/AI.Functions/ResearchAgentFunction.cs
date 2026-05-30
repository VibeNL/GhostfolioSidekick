using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.AI.Functions
{
	public partial class ResearchAgentFunction(
		IGoogleSearchService searchService,
		IChatClient chatService,
		ModelInfo modelInfo,
		AgentLogger agentLogger)
	{
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
					agentLogger.StartFunction($"{nameof(MultiStepResearch)} Summarizing search result {aspect} {++i}");
					var sanitizedContent = SanitizeText(result.Content ?? string.Empty);
					var synthesisPrompt = TruncatePrompt($"Synthesize the following research result into a concise summary. {sanitizedContent}", modelInfo.MaxTokens);
					var chatResult = await chatService.GetResponseAsync(synthesisPrompt);
					perResultSummaries.Add(chatResult.Text);
				}

				// Synthesize aspect summary from per-result summaries
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing aspect summary for: {aspect}");

				var aspectSynthesisPrompt = TruncatePrompt($"Synthesize the following summaries for aspect '{aspect}' into a concise aspect summary.\n{string.Join(Environment.NewLine, perResultSummaries)}", modelInfo.MaxTokens);
				var aspectChatResult = await chatService.GetResponseAsync(aspectSynthesisPrompt);
				aspectSummaries.Add(aspectChatResult.Text);
			}

			// Synthesize the aspect summaries into a final summary
			agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing final summary");

			var finalPrompt = TruncatePrompt($"Synthesize the following aspect summaries into a concise overall summary.\n{string.Join(Environment.NewLine, aspectSummaries)}", modelInfo.MaxTokens);
			var finalChatResult = await chatService.GetResponseAsync(finalPrompt);
			return finalChatResult.Text;
		}

		internal static string SanitizeText(string input)
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

		internal static string TruncatePrompt(string prompt, int maxTokens)
		{
			if (string.IsNullOrEmpty(prompt)) return string.Empty;
			return prompt.Length > maxTokens ? prompt[..maxTokens] : prompt;
		}
	}
}
