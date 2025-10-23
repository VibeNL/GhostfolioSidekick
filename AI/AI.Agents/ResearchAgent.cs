using AI.Functions.OnlineSearch;
using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.AI.Agents
{
	public static class ResearchAgent
	{
		private static string GetSystemPrompt()
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are ResearchAgent AI — a smart financial assistant.");
			sb.AppendLine($"Today is {DateTime.Now:yyyy-MM-dd}.");
			sb.AppendLine("You may call functions if needed.");
			sb.AppendLine("You can make multiple related function calls to gather comprehensive information.");
			sb.AppendLine("When you get function results, you should analyze them and provide a helpful summary.");
			sb.AppendLine("For complex research tasks, you can use the multi_step_research function to perform a series of research steps.");
			return sb.ToString();
		}

		public static ChatCompletionAgent Create(ICustomChatClient webChatClient, IServiceProvider serviceProvider)
		{
			string researchAgent = GetSystemPrompt();
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var searchService = serviceProvider.GetRequiredService<GoogleSearchService>();
			var agentlogger = serviceProvider.GetRequiredService<AgentLogger>();
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromObject(new ResearchAgentFunction(searchService, webChatClient.AsChatCompletionService(), agentlogger));

			var agent = new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Instructions = researchAgent,
				InstructionsRole = AuthorRole.System,
				Kernel = functionCallingkernel,
				Description = "A researcher that can access real-time data on the internet. Also can query recent financial news and perform multi-step research.",
				Arguments = new KernelArguments(new PromptExecutionSettings()
				{
					FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
				})
			};

			return agent;
		}
	}

	public class ResearchAgentFunction(GoogleSearchService searchService, IChatCompletionService chatService, AgentLogger agentLogger)
	{
		private async Task<string> SummarizeSearchResults(SearchResults results)
		{
			if (results.Items.Count == 0)
			{
				return "No results to summarize.";
			}

			var prompt = new StringBuilder();
			prompt.AppendLine($"Please analyze and summarize these search results for the query: \"{results.Query}\"");
			prompt.AppendLine("Extract key information, identify common themes, and note any significant findings.");
			prompt.AppendLine("Focus on providing accurate and useful information that answers the query.");

			foreach (var item in results.Items.Take(3))
			{
				prompt.AppendLine($"\nTitle: {item.Title}");
				if (!string.IsNullOrEmpty(item.Content) && item.Content.Length > 100)
				{
					prompt.AppendLine($"Content: {item.Content[..Math.Min(500, item.Content.Length)]}...");
				}
			}

			var chatHistory = new ChatHistory();
			chatHistory.AddSystemMessage("You are a helpful research assistant that can synthesize information from multiple sources.");
			chatHistory.AddUserMessage(prompt.ToString());

			var result = await chatService.GetChatMessageContentAsync(chatHistory);
			return result.Content ?? "No summary content available.";
		}

		/// <summary>
		/// Generates an optimal search query using the LLM's understanding of search effectiveness
		/// </summary>
		private async Task<string> GenerateSearchQuery(string topic, string aspect)
		{
			// Current date info for time-sensitive queries
			var currentYear = DateTime.Now.Year;
			var currentDate = DateTime.Now.ToString("yyyy-MM-dd");

			var chatHistory = new ChatHistory();

			// The system prompt provides detailed guidance on creating effective search queries
			chatHistory.AddSystemMessage(@"You are an expert search query generator that creates highly effective queries for web searches.
Your goal is to formulate queries that will return the most relevant and useful information.

Follow these principles:
1. Include key concepts and specific terms that will yield precise results
2. Use quotes around exact phrases when appropriate
3. Include relevant time periods or years when the information should be recent
4. For financial or market topics, include specific terminology professionals would use
5. Omit unnecessary words like 'how', 'why', 'what is', etc. that don't enhance search precision
6. Include synonyms for important terms when relevant
7. Use operators like AND or OR if they enhance query precision
8. For time-sensitive topics, include the current year to get recent information
9. Keep queries under 150 characters to ensure search engine compatibility

YOU MUST ONLY RESPOND WITH THE EXACT SEARCH QUERY TEXT. No explanations or formatting.");

			// The prompt is structured to elicit an optimal search query
			chatHistory.AddUserMessage($@"Generate the most effective search query to find accurate and relevant information about '{aspect}' as it relates to '{topic}'. 
Current date: {currentDate}
Current year: {currentYear}

The query should lead to high-quality, factual information that would help someone understand this specific aspect of the topic.
If recency is important, incorporate the year {currentYear} in your query.");

			// Generate the optimized query
			var response = await chatService.GetChatMessageContentAsync(chatHistory);
			var optimizedQuery = response.Content?.Trim() ?? $"{topic} {aspect}";

			// Safety checks and constraints
			if (string.IsNullOrWhiteSpace(optimizedQuery))
			{
				return $"{topic} {aspect}";
			}

			// Ensure the query isn't too long for search engines
			if (optimizedQuery.Length > 150)
			{
				optimizedQuery = optimizedQuery[..150];
			}

			// Make sure the core topic is included in the query
			if (!optimizedQuery.Contains(topic, StringComparison.OrdinalIgnoreCase))
			{
				// Rather than simply appending the topic, try to integrate it more naturally
				var rewriteHistory = new ChatHistory();
				rewriteHistory.AddSystemMessage("You are an expert at refining search queries. Respond only with the rewritten query.");
				rewriteHistory.AddUserMessage($"This search query needs to include the term '{topic}' but currently doesn't: \"{optimizedQuery}\". Rewrite the query to include this term while maintaining its effectiveness. Make sure the query stays under 150 characters.");

				var rewriteResponse = await chatService.GetChatMessageContentAsync(rewriteHistory);
				var rewrittenQuery = rewriteResponse.Content?.Trim();

				// Only use the rewritten query if it actually contains the topic and isn't empty
				if (!string.IsNullOrWhiteSpace(rewrittenQuery) && rewrittenQuery.Contains(topic, StringComparison.OrdinalIgnoreCase))
				{
					optimizedQuery = rewrittenQuery;
				}
				else
				{
					// Fallback if rewrite failed
					optimizedQuery = $"{topic} {optimizedQuery}";
				}
			}

			return ChatMessageContentHelper.ToDisplayText(optimizedQuery);
		}

		/// <summary>
		/// Evaluates search results and suggests alternative search queries if needed
		/// </summary>
		private async Task<string> SuggestAlternativeQuery(string originalQuery, string topic, string aspect)
		{
			var chatHistory = new ChatHistory();
			chatHistory.AddSystemMessage("You are a search query optimization expert. Your task is to suggest alternative search queries when the original query doesn't yield good results.");
			chatHistory.AddUserMessage($"The search query \"{originalQuery}\" for researching \"{aspect}\" of \"{topic}\" didn't return useful results. Suggest an alternative search query that might work better. Only provide the query text with no explanation or additional text.");

			var response = await chatService.GetChatMessageContentAsync(chatHistory);
			var alternativeQuery = response.Content?.Trim();

			if (string.IsNullOrWhiteSpace(alternativeQuery))
			{
				// If LLM failed to provide an alternative, create a simpler version
				return $"{topic} {aspect} information";
			}

			return ChatMessageContentHelper.ToDisplayText(alternativeQuery);
		}

		[KernelFunction("multi_step_research")]
		[Description("Perform multi-step research on a topic by making multiple queries and synthesizing the results")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task<string> MultiStepResearch(
			[Description("The topic to research")] string topic,
			[Description("Specific aspects of the topic to research. Should be in natural language")] string[] aspects)
		{
			agentLogger.StartFunction(nameof(MultiStepResearch));

			if (string.IsNullOrWhiteSpace(topic))
			{
				return "No research topic provided.";
			}

			if (aspects == null || aspects.Length == 0)
			{
				// Generate research aspects tailored to the topic using the LLM
				var chatHistory = new ChatHistory();
				chatHistory.AddSystemMessage(@"You are a research planning expert who can identify the most important aspects to investigate about a topic.
For financial topics, consider market performance, trends, economic impact, and investment considerations.
For companies, consider business model, financials, competitive landscape, and future outlook.
For products, consider features, market position, comparison with alternatives, and reception.
Respond with a JSON array of 3-5 clear and specific aspects to research.");

				chatHistory.AddUserMessage($"What are the most important aspects to research about '{topic}'? Focus on aspects that would provide valuable and comprehensive understanding. Respond with ONLY a JSON array of strings.");

				var aspectResponse = await chatService.GetChatMessageContentAsync(chatHistory);
				var aspectContent = aspectResponse.Content ?? "[]";

				// Extract the JSON array from the response
				try
				{
					aspectContent = aspectContent.Trim();
					if (aspectContent.StartsWith("```json"))
					{
						aspectContent = aspectContent[7..];
					}
					if (aspectContent.StartsWith("```"))
					{
						aspectContent = aspectContent[3..];
					}
					if (aspectContent.EndsWith("```"))
					{
						aspectContent = aspectContent[..^3];
					}
					aspects = JsonSerializer.Deserialize<string[]>(aspectContent) ?? ["overview", "recent developments", "analysis"];
				}
				catch
				{
					aspects = ["overview", "recent developments", "analysis"];
				}
			}

			// Now perform the research on each aspect
			var allResults = new Dictionary<string, string>();
			var researchBuilder = new StringBuilder();
			researchBuilder.AppendLine($"# Multi-Step Research on: {topic}");
			researchBuilder.AppendLine();

			foreach (var aspect in aspects)
			{
				researchBuilder.AppendLine($"## Researching: {aspect}");

				// Generate an optimized search query for this specific aspect and topic
				var optimizedQuery = await GenerateSearchQuery(topic, aspect);

				// Try the optimized query
				var results = await searchService.SearchAsync(optimizedQuery);

				// If no results, try generating an alternative query
				if (results == null || results.Count == 0)
				{
					var alternativeQuery = await SuggestAlternativeQuery(optimizedQuery, topic, aspect);
					results = await searchService.SearchAsync(alternativeQuery);

					// If still no results, try a simple fallback query as a last resort
					if (results == null || results.Count == 0)
					{
						var fallbackQuery = $"{topic} {aspect}";
						results = await searchService.SearchAsync(fallbackQuery);
					}
				}

				if (results == null || results.Count == 0)
				{
					researchBuilder.AppendLine("No results found for this aspect.");
					continue;
				}

				var resultsForSummarization = new SearchResults
				{
					Query = $"{topic} - {aspect}",
					Items = [.. results.Select(r => new SearchResultItem
					{
						Title = r.Title ?? "No title",
						Link = r.Link ?? string.Empty,
						Content = r.Content ?? r.Snippet ?? "No content available"
					})]
				};

				var summary = await SummarizeSearchResults(resultsForSummarization);
				researchBuilder.AppendLine(summary);
				researchBuilder.AppendLine();

				allResults[aspect] = summary;
			}

			// Check if we have any results at all
			if (allResults.Count == 0)
			{
				return $"# Research on {topic}\n\nNo results found for any of the research aspects. Please try a different topic or check your internet connection.";
			}

			// Final synthesis of all aspects
			researchBuilder.AppendLine("# Research Synthesis");

			var synthesisPromptBuilder = new StringBuilder();
			synthesisPromptBuilder.AppendLine($"Please synthesize the following research findings on '{topic}':");

			foreach (var kvp in allResults)
			{
				synthesisPromptBuilder.AppendLine($"\n## {kvp.Key}");
				synthesisPromptBuilder.AppendLine(kvp.Value);
			}

			var synChatHistory = new ChatHistory();
			synChatHistory.AddSystemMessage(@"You are a research synthesis expert who can integrate multiple aspects of a topic into a coherent analysis.
Your task is to:
1. Identify key themes across all the research aspects
2. Note connections between different aspects
3. Highlight the most important findings and insights
4. Address any contradictions or differences in the information
5. Provide a balanced and comprehensive overview of the topic
Maintain a professional tone suitable for financial and investment contexts.");

			synChatHistory.AddUserMessage(synthesisPromptBuilder.ToString());

			var synthesisResponse = await chatService.GetChatMessageContentAsync(synChatHistory);
			var synthesis = synthesisResponse.Content ?? "Unable to synthesize research.";

			researchBuilder.AppendLine(synthesis);

			return researchBuilder.ToString();
		}
	}

	public class SearchResults
	{
		public string Query { get; set; } = string.Empty;
		public List<SearchResultItem> Items { get; set; } = [];
	}

	public class SearchResultItem
	{
		public string Title { get; set; } = string.Empty;
		public string Link { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
	}
}
