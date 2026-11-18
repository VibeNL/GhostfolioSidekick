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
			[Description("Specific aspects of the topic to research. Should be in natural language")] string[] aspects,
			CancellationToken cancellationToken = default)
		{
			agentLogger.StartFunction(nameof(MultiStepResearch));

			if (string.IsNullOrWhiteSpace(topic))
			{
				return "No research topic was provided. Please call multi_step_research with a non-empty topic.";
			}

			if (aspects is null || aspects.Length == 0)
			{
				return "No research aspects were provided. Please call multi_step_research with at least one aspect to research.";
			}

			var aspectSummaries = new List<string>();
			foreach (var aspect in aspects)
			{
				var query = $"{topic} - {aspect}";
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Searching for: {query}");
				var searchResult = await searchService.SearchAsync(query);
				var topResults = searchResult.Take(3).ToList();

				var perResultSummaries = new List<string>();
				if (topResults.Count == 0)
				{
					perResultSummaries.Add($"No web search results were found for '{query}'.");
				}
				else
				{
					int i = 0;
					foreach (var result in topResults)
					{
						agentLogger.StartFunction($"{nameof(MultiStepResearch)} Summarizing search result {aspect} {++i}");
						var sanitizedContent = SanitizeText(result.Content ?? string.Empty);
						var synthesisPrompt = TruncatePrompt($"Synthesize the following research result into a concise summary. {sanitizedContent}", modelInfo.MaxTokens);
						var chatResult = await chatService.GetResponseAsync(synthesisPrompt, cancellationToken: cancellationToken);
						perResultSummaries.Add(chatResult.Text);
					}
				}

				// Synthesize aspect summary from per-result summaries
				agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing aspect summary for: {aspect}");

				var aspectSynthesisPrompt = TruncatePrompt($"Synthesize the following summaries for aspect '{aspect}' into a concise aspect summary.\n{string.Join(Environment.NewLine, perResultSummaries)}", modelInfo.MaxTokens);
				var aspectChatResult = await chatService.GetResponseAsync(aspectSynthesisPrompt, cancellationToken: cancellationToken);
				aspectSummaries.Add(aspectChatResult.Text);
			}

			// Synthesize the aspect summaries into a final summary
			agentLogger.StartFunction($"{nameof(MultiStepResearch)} Synthesizing final summary");

			var finalPrompt = TruncatePrompt($"Synthesize the following aspect summaries into a concise overall summary.\n{string.Join(Environment.NewLine, aspectSummaries)}", modelInfo.MaxTokens);
			var finalChatResult = await chatService.GetResponseAsync(finalPrompt, cancellationToken: cancellationToken);
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
			if (prompt.Length <= maxTokens) return prompt;

			var truncated = prompt[..maxTokens];
			// Don't split a UTF-16 surrogate pair at the cut point.
			if (truncated.Length > 0 && char.IsHighSurrogate(truncated[^1]))
			{
				truncated = truncated[..^1];
			}

			return truncated;
		}
	}
}
