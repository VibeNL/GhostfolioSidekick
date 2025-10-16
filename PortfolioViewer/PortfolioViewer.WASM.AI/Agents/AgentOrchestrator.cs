using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading;
using System.Runtime.CompilerServices;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class AgentOrchestrator
	{
		private const string SafeParameterNames = "history";
		private readonly Agent defaultAgent;
		private readonly List<Agent> agents;
		private readonly IAgentGroupChatShim groupChatShim;
		private readonly AgentLogger logger;

		// Default constructor used in production
		public AgentOrchestrator(IServiceProvider serviceProvider, AgentLogger logger)
		{
			IKernelBuilder builder = Kernel.CreateBuilder();
			var webChatClient = serviceProvider.GetRequiredService<IWebChatClient>();
			builder.Services.AddSingleton((s) => webChatClient.AsChatCompletionService());

			var kernel = builder.Build();

			var researchAgent = ResearchAgent.Create(webChatClient, serviceProvider);
			defaultAgent = GhostfolioSidekick.Create(webChatClient, new[] { researchAgent });

			agents =
			[
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
					safeParameterNames: SafeParameterNames);
			var selectionFunction = WrapWithLogging(rawSelectionFunction, "SelectionStrategy");

			// Define the selection strategy
			KernelFunctionSelectionStrategy selectionStrategy =
			  new(selectionFunction, kernel)
			  {
				  // Always start with the writer agent.
				  InitialAgent = defaultAgent,
				  // Parse the function response.
				  ResultParser = DetermineNextAgentWithLogger,
				  // The prompt variable name for the history argument.
				  HistoryVariableName = SafeParameterNames,
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
					safeParameterNames: SafeParameterNames);
			var terminationFunction = WrapWithLogging(rawTerminationFunction, "TerminationStrategy");

			// Define the termination strategy
			KernelFunctionTerminationStrategy terminationStrategy =
			  new(terminationFunction, kernel)
			  {
				  // Only the reviewer may give approval.
				  Agents = new[] { defaultAgent },
				  // Parse the function response.
				  ResultParser = DetermineTermination,
				  // The prompt variable name for the history argument.
				  HistoryVariableName = SafeParameterNames,
				  // Save tokens by not including the entire history in the prompt
				  HistoryReducer = new ChatHistoryTruncationReducer(1),
				  // Limit total number of turns no matter what
				  MaximumIterations = 10,
				  AutomaticReset = true,
			  };

			var groupChat = new AgentGroupChat([.. agents])
			{
				ExecutionSettings = new AgentGroupChatSettings
				{
					TerminationStrategy = terminationStrategy,
					SelectionStrategy = selectionStrategy
				},
			};

			this.groupChatShim = new DefaultAgentGroupChatShim(groupChat);
			this.logger = logger;
		}

		// Public constructor used by tests to inject a shim
		public AgentOrchestrator(IAgentGroupChatShim shim, AgentLogger logger)
		{
			this.groupChatShim = shim ?? throw new ArgumentNullException(nameof(shim));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.agents = [];
			this.defaultAgent = new ChatCompletionAgent { Name = "TestAgent", Description = "Test agent", InstructionsRole = AuthorRole.System };
			this.agents.Add(this.defaultAgent);
		}

		public async Task<IReadOnlyCollection<ChatMessageContent>> History()
		{
			// With the following code to manually collect the messages into a list:
			var messages = new List<ChatMessageContent>();
			await foreach (var message in groupChatShim.GetChatMessagesAsync())
			{
				messages.Add(new ChatMessageContent(message.Role, message.Content ?? string.Empty) { AuthorName = message.AuthorName });
			}

			return messages.AsEnumerable().Reverse().Where(x => x.Content != null).ToList();
		}

		public async IAsyncEnumerable<ChatMessageContent> AskQuestion(string input)
		{
			logger.StartAgent(defaultAgent?.Name ?? "<????>");
			groupChatShim.AddChatMessage(new SimpleStreamingMessage(AuthorRole.User, input, "User"));

			// Run the group chat
			var result = groupChatShim.InvokeStreamingAsync();

			await foreach (var update in result)
			{
				yield return new ChatMessageContent(update.Role, update.Content ?? string.Empty) { AuthorName = update.AuthorName };
			}

			logger.StartAgent(string.Empty);
		}

		private static bool DetermineTermination(FunctionResult result)
		{
			_ = ChatMessageContentHelper.ToDisplayText(result.GetValue<string>());
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

		private sealed class DefaultAgentGroupChatShim(AgentGroupChat inner) : IAgentGroupChatShim
		{
			public void AddChatMessage(SimpleStreamingMessage message) => inner.AddChatMessage(new ChatMessageContent(message.Role, message.Content ?? string.Empty) { AuthorName = message.AuthorName });
			public async IAsyncEnumerable<SimpleStreamingMessage> InvokeStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await foreach (var s in inner.InvokeStreamingAsync(cancellationToken))
				{
					// Map StreamingChatMessageContent to SimpleStreamingMessage
					yield return new SimpleStreamingMessage(s.Role ?? AuthorRole.Assistant, s.Content, s.AuthorName);
				}
			}

			public async IAsyncEnumerable<SimpleStreamingMessage> GetChatMessagesAsync()
			{
				await foreach (var s in inner.GetChatMessagesAsync())
				{
					yield return new SimpleStreamingMessage(s.Role, s.Content, s.AuthorName);
				}
			}
		}
	}
}
