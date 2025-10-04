using Microsoft.Extensions.AI;
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
		private readonly AgentLogger logger;

		public AgentOrchestrator(IServiceProvider serviceProvider, AgentLogger logger)
		{
			IKernelBuilder builder = Kernel.CreateBuilder();
			var webChatClient = serviceProvider.GetRequiredService<IWebChatClient>();
			builder.Services.AddSingleton((s) => webChatClient.AsChatCompletionService());
			
			kernel = builder.Build();

			var researchAgent = ResearchAgent.Create(webChatClient, serviceProvider);
			defaultAgent = GhostfolioSidekick.Create(webChatClient, [researchAgent]);

			this.agents = [
				defaultAgent,
				researchAgent
			];

			// Define a kernel function for the selection strategy
			KernelFunction rawSelectionFunction =
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
			var selectionFunction = WrapWithLogging(rawSelectionFunction, "SelectionStrategy");

			// Define the selection strategy
			KernelFunctionSelectionStrategy selectionStrategy =
			  new(selectionFunction, kernel)
			  {
				  // Always start with the writer agent.
				  InitialAgent = defaultAgent,
				  // Parse the function response.
				  ResultParser = (result) => DetermineNextAgentWithLogger(result),
				  // The prompt variable name for the history argument.
				  HistoryVariableName = "history",
				  // Save tokens by not including the entire history in the prompt
				  HistoryReducer = new ChatHistoryTruncationReducer(3),
			  };

			KernelFunction rawTerminationFunction =
				AgentGroupChat.CreatePromptFunctionForStrategy(
					$$$"""
					Determine if the conversation has ended. Then respond with 'User' 
					In case another agent should take the next turn, respond with the name of that agent.

					History:
					{{$history}}
					""",
					safeParameterNames: "history");
			var terminationFunction = WrapWithLogging(rawTerminationFunction, "TerminationStrategy");

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
				},
				//LoggerFactory = logger,				
			};

			this.logger = logger;
		}

		public async Task<IReadOnlyCollection<ChatMessageContent>> History()
		{
			// With the following code to manually collect the messages into a list:
			var messages = new List<ChatMessageContent>();
			await foreach (var message in groupChat.GetChatMessagesAsync())
			{
				messages.Add(message);
			}

			return messages.AsEnumerable().Reverse().Where(x => x.Content != null).ToList();
		}

		public async IAsyncEnumerable<StreamingChatMessageContent> AskQuestion(string input)
		{
			logger.StartAgent(defaultAgent?.Name ?? "<????>");
			groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input) { AuthorName = "User" });

			// Run the group chat
			var result = groupChat.InvokeStreamingAsync();

			await foreach (var update in result)
			{
				yield return update;
			}

			logger.StartAgent(string.Empty);
		}

		private static bool DetermineTermination(FunctionResult result)
		{
			var value = ChatMessageContentHelper.ToDisplayText(result.GetValue<string>());
			return result.GetValue<string>()?.Contains("User", StringComparison.OrdinalIgnoreCase) ?? false;
		}

		private string DetermineNextAgentWithLogger(FunctionResult result)
		{
			var nextAgent = DetermineNextAgent(result);
			logger.StartAgent(nextAgent);
			return nextAgent;
		}

		private string DetermineNextAgent(FunctionResult result)
		{
			var value = ChatMessageContentHelper.ToDisplayText(result.GetValue<string>());
			var splitted = value?.Split(' ');
			var lastWord = splitted?.LastOrDefault()?.Trim();

			if (lastWord != null && agents.Exists(x => string.Equals(x.Name, lastWord, StringComparison.InvariantCultureIgnoreCase)))
			{
				return lastWord;
			}

			return defaultAgent.Name ?? throw new NotSupportedException();
		}

		private KernelFunction WrapWithLogging(KernelFunction originalFunction, string name)
		{
			return KernelFunctionFactory.CreateFromMethod(async (Kernel kernel, KernelArguments args) =>
			{
				logger.StartAgent(name);
				var result = await originalFunction.InvokeAsync(kernel, args);
				return result;
			});
		}
	}
}
