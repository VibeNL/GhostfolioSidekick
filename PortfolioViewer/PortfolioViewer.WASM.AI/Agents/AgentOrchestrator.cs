using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class AgentOrchestrator
	{
		
		private readonly Kernel kernel;
		private readonly Agent defaultAgent;
		private readonly List<Agent> agents;
		private readonly AgentGroupChat groupChat;

		public AgentOrchestrator(IWebChatClient webChatClient)
		{
			IKernelBuilder builder = Kernel.CreateBuilder();
			builder.Services.AddScoped<IChatCompletionService>((s) => webChatClient.AsChatCompletionService());
			kernel = builder.Build();

			IKernelBuilder thinkBuilder = Kernel.CreateBuilder();
			thinkBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.EnableThinking = true;
				return client.AsChatCompletionService();
			});
			var thinkingKernel = thinkBuilder.Build();

			var researchAgent = ResearchAgent.Create(thinkingKernel);
			defaultAgent = GhostfolioSidekick.Create(thinkingKernel, [researchAgent]);

			this.agents = [
				defaultAgent,
				researchAgent
			];

			// Define a kernel function for the selection strategy
			KernelFunction selectionFunction =
				AgentGroupChat.CreatePromptFunctionForStrategy(
					$$$"""
						Determine which participant takes the next turn in a conversation based on the the most recent participant.
						State only the name of the participant to take the next turn.
						No participant should take more than one turn in a row.
						When the input from the User is required, please select User

						Choose only from these participants:
						- User
						{{{string.Join(Environment.NewLine, agents.Select(x => $"{x.Name}:{x.Description}"))}}}

						History:
						{{$history}}
						""",
					safeParameterNames: "history");

			// Define the selection strategy
			KernelFunctionSelectionStrategy selectionStrategy =
			  new(selectionFunction, kernel)
			  {
				  // Always start with the writer agent.
				  InitialAgent = defaultAgent,
				  // Parse the function response.
				  ResultParser = (result) => DetermineNextAgent(result),
				  // The prompt variable name for the history argument.
				  HistoryVariableName = "history",
				  // Save tokens by not including the entire history in the prompt
				  HistoryReducer = new ChatHistoryTruncationReducer(3),
			  };

			KernelFunction terminationFunction =
				AgentGroupChat.CreatePromptFunctionForStrategy(
					$$$"""
					Determine if the conversation has ended. Then respond with 'User' 
					In case another agent should take the next turn, respond with the name of that agent.

					History:
					{{$history}}
					""",
					safeParameterNames: "history");

			// Define the termination strategy
			KernelFunctionTerminationStrategy terminationStrategy =
			  new(terminationFunction, kernel)
			  {
				  // Only the reviewer may give approval.
				  Agents = [defaultAgent],
				  // Parse the function response.
				  ResultParser = (result) =>
					DetermineTermination(result),
				  // The prompt variable name for the history argument.
				  HistoryVariableName = "history",
				  // Save tokens by not including the entire history in the prompt
				  HistoryReducer = new ChatHistoryTruncationReducer(1),
				  // Limit total number of turns no matter what
				  MaximumIterations = 10,
				  AutomaticReset = true,
				  
			  };

			groupChat = new AgentGroupChat([.. agents])
			{
				ExecutionSettings = new AgentGroupChatSettings
				{
					TerminationStrategy = terminationStrategy,
					SelectionStrategy = selectionStrategy
				}
			};
		}

		public async Task<IReadOnlyCollection<ChatMessageContent>> History()
		{
			// History is stored in reverse order, so we need to reverse it to display it correctly.
			return await groupChat.GetChatMessagesAsync().Reverse().Where(x => x.Content != null).ToListAsync();
		}

		public async IAsyncEnumerable<StreamingChatMessageContent> AskQuestion(string input)
		{
			groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input) { AuthorName = "User" });

			// Run the group chat
			var result = groupChat.InvokeStreamingAsync();

			await foreach (var update in result)
			{
				yield return update;
			}
		}

		private static bool DetermineTermination(FunctionResult result)
		{
			return result.GetValue<string>()?.Contains("User", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		private string DetermineNextAgent(FunctionResult result)
		{
			var lastWord = result.GetValue<string>()?.Split(' ').LastOrDefault();

			if (lastWord != null && agents.Exists(x => x.Name == lastWord))
			{
				return lastWord;
			}

			return defaultAgent.Name ?? throw new NotSupportedException();
		}
	}
}
