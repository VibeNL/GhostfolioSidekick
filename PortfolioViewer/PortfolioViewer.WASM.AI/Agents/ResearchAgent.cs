using GhostfolioSidekick.ExternalDataProvider.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
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

		public static ChatCompletionAgent Create(IWebChatClient webChatClient, IServiceProvider serviceProvider)
		{
			string researchAgent = GetSystemPrompt();
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var searchService = serviceProvider.GetRequiredService<GoogleSearchService>();
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromObject(new ResearchAgentFunction(searchService, webChatClient.AsChatCompletionService()));

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

	public class ResearchAgentFunction
	{
		private readonly GoogleSearchService _searchService;
		private readonly IChatCompletionService _chatService;

		public ResearchAgentFunction(GoogleSearchService searchService, IChatCompletionService chatService)
		{
			_searchService = searchService;
			_chatService = chatService;
		}

		[KernelFunction("query_internet")]
		[Description("Search the internet with a query")]
		public async Task<string> QueryInternet(string query)
		{
			var results = await _searchService.SearchAsync(query);
			
			if (results.Count == 0)
			{
				return "No results found.";
			}
			
			var resultsForSummarization = new SearchResults
			{
				Query = query,
				Items = results.Select(r => new SearchResultItem
				{
					Title = r.Title,
					Link = r.Link,
					Snippet = r.Snippet,
					Content = r.Content
				}).ToList()
			};
			
			// Get the raw results first
			var sb = new StringBuilder();
			sb.AppendLine($"Search results for query: \"{query}\"");
			sb.AppendLine();
			
			foreach (var result in results)
			{
				sb.AppendLine($"Title: {result.Title}");
				sb.AppendLine($"Link: {result.Link}");
				sb.AppendLine($"Snippet: {result.Snippet}");
				if (!string.IsNullOrEmpty(result.Content))
				{
					sb.AppendLine($"Content: {result.Content.Substring(0, Math.Min(1000, result.Content.Length))}...");
				}
				sb.AppendLine();
			}

			// Call the summarize function to get a summary of the results
			var summary = await SummarizeSearchResults(resultsForSummarization);
			sb.AppendLine();
			sb.AppendLine("SUMMARY OF SEARCH RESULTS:");
			sb.AppendLine(summary);
			
			return sb.ToString();
		}

		[KernelFunction("get_stock_price")]
		[Description("Get the stock price for a given subject")]
		public Task<string> GetStockPrice(string subject, string date)
		{
			return Task.FromResult($"The price is $400 for {date}");
		}
		
		[KernelFunction("summarize_content")]
		[Description("Summarize the provided content")]
		public async Task<string> SummarizeContent(string content, string topic = "")
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return "No content to summarize.";
			}

			var prompt = new StringBuilder();
			prompt.AppendLine("Please summarize the following content:");
			if (!string.IsNullOrWhiteSpace(topic))
			{
				prompt.AppendLine($"Focus on information related to: {topic}");
			}
			prompt.AppendLine(content);

			var chatHistory = new ChatHistory();
			chatHistory.AddSystemMessage("You are a helpful assistant that summarizes content concisely and accurately.");
			chatHistory.AddUserMessage(prompt.ToString());

			var response = await _chatService.GetResponseAsync(chatHistory);
			return response.Content;
		}
		
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
				prompt.AppendLine($"Snippet: {item.Snippet}");
				if (!string.IsNullOrEmpty(item.Content) && item.Content.Length > 100)
				{
					prompt.AppendLine($"Content: {item.Content.Substring(0, Math.Min(500, item.Content.Length))}...");
				}
			}

			var chatHistory = new ChatHistory();
			chatHistory.AddSystemMessage("You are a helpful research assistant that can synthesize information from multiple sources.");
			chatHistory.AddUserMessage(prompt.ToString());

			var response = await _chatService.GetResponseAsync(chatHistory);
			return response.Content;
		}
		
		[KernelFunction("multi_step_research")]
		[Description("Perform multi-step research on a topic by making multiple queries and synthesizing the results")]
		public async Task<string> MultiStepResearch(string topic, [Description("Specific aspects of the topic to research")] string[] aspects)
		{
			if (string.IsNullOrWhiteSpace(topic))
				return "No research topic provided.";
				
			if (aspects == null || aspects.Length == 0)
			{
				// Generate default aspects if none provided
				var defaultAspectPrompt = $"What are 3-5 important aspects to research about '{topic}'? Respond with just a JSON array of strings.";
				
				var chatHistory = new ChatHistory();
				chatHistory.AddSystemMessage("You are a research planning assistant. Respond only with the requested JSON format.");
				chatHistory.AddUserMessage(defaultAspectPrompt);
				
				var aspectResponse = await _chatService.GetResponseAsync(chatHistory);
				var aspectContent = aspectResponse.Content ?? "[]";
				
				// Extract the JSON array from the response
				try
				{
					aspectContent = aspectContent.Trim();
					if (aspectContent.StartsWith("```json"))
					{
						aspectContent = aspectContent.Substring(7);
					}
					if (aspectContent.StartsWith("```"))
					{
						aspectContent = aspectContent.Substring(3);
					}
					if (aspectContent.EndsWith("```"))
					{
						aspectContent = aspectContent.Substring(0, aspectContent.Length - 3);
					}
					aspects = JsonSerializer.Deserialize<string[]>(aspectContent) ?? new string[] { "overview", "recent developments", "analysis" };
				}
				catch
				{
					aspects = new string[] { "overview", "recent developments", "analysis" };
				}
			}
			
			// Now perform the research on each aspect
			var allResults = new Dictionary<string, string>();
			var sb = new StringBuilder();
			sb.AppendLine($"# Multi-Step Research on: {topic}");
			sb.AppendLine();
			
			foreach (var aspect in aspects)
			{
				var query = $"{topic} {aspect}";
				sb.AppendLine($"## Researching: {aspect}");
				
				var results = await _searchService.SearchAsync(query);
				if (results.Count == 0)
				{
					sb.AppendLine("No results found for this aspect.");
					continue;
				}
				
				var resultsForSummarization = new SearchResults
				{
					Query = query,
					Items = results.Select(r => new SearchResultItem
					{
						Title = r.Title,
						Link = r.Link,
						Snippet = r.Snippet,
						Content = r.Content
					}).ToList()
				};
				
				var summary = await SummarizeSearchResults(resultsForSummarization);
				sb.AppendLine(summary);
				sb.AppendLine();
				
				allResults[aspect] = summary;
			}
			
			// Final synthesis of all aspects
			sb.AppendLine("# Research Synthesis");
			
			var synthesisPrompt = new StringBuilder();
			synthesisPrompt.AppendLine($"Please synthesize the following research findings on '{topic}':");
			
			foreach (var kvp in allResults)
			{
				synthesisPrompt.AppendLine($"\n## {kvp.Key}");
				synthesisPrompt.AppendLine(kvp.Value);
			}
			
			var synChatHistory = new ChatHistory();
			synChatHistory.AddSystemMessage("You are a research synthesis expert who can integrate multiple aspects of a topic into a coherent analysis.");
			synChatHistory.AddUserMessage(synthesisPrompt.ToString());
			
			var synthesisResponse = await _chatService.GetResponseAsync(synChatHistory);
			var synthesis = synthesisResponse.Content ?? "Unable to synthesize research.";
			
			sb.AppendLine(synthesis);
			
			return sb.ToString();
		}
	}
	
	public class SearchResults
	{
		public string Query { get; set; }
		public List<SearchResultItem> Items { get; set; } = new();
	}
	
	public class SearchResultItem
	{
		public string Title { get; set; }
		public string Link { get; set; }
		public string Content { get; set; }
	}
}
