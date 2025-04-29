using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class PortfolioAgentGroupChat(string name, IAgent[] agents, ILogger<PortfolioAgentGroupChat> logger)
	{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
		public async Task<CustomAgentGroupChat> Initialize(Kernel kernel)
		{
			// Use a List instead of an array to avoid unsafe array conversions
			var chatAgents = new Dictionary<IAgent, Agent>();
			foreach (var agent in agents)
			{
				chatAgents.Add(agent, await agent.Initialize(kernel));
			}

			string prompt = $@"""
You are managing a multi-agent conversation. Your task is to decide **which participant should respond next** based only on the most recent message in the conversation.

Choose **only one** from the list below. Respond with the exact `name` of the participant. Do not include any extra text or explanation.

Participants:
""" +
string.Join(Environment.NewLine, agents.Select(x => $"- name: '{x.Name}' description: '{x.Description}'")) +
$@"

Rules:
- Use the **description** to decide which agent is most suitable.
- If a question relates to personal financial data, choose the portfolio query agent.
- If it's about general finance or investments, choose the financial expert.
- If no agent is appropriate, choose the fallback/default agent.
- Return only the agent's **name**, nothing else.

Conversation history:
{{{{$history}}}}
""";

			KernelFunction selectionFunction =
				AgentGroupChat.CreatePromptFunctionForStrategy(
					prompt,
					safeParameterNames: "history");

			// Define the selection strategy
			var defaultAgent = chatAgents.SingleOrDefault(x => x.Key.InitialAgent);
			KernelFunctionSelectionStrategy selectionStrategy =
			  new(selectionFunction, kernel)
			  {
				  // Always start with the writer agent.
				  InitialAgent = defaultAgent.Value,
				  // Parse the function response.
				  ResultParser = (result) =>
				  {
					  var prompt = result.RenderedPrompt;
					  var parsedResult = result.GetValue<string>()?.Trim();
					  logger.LogInformation($"Selection Function Result: {parsedResult}");

					  if (string.IsNullOrWhiteSpace(parsedResult))
					  {
						  logger.LogInformation("Invalid result. Falling back to default.");
						  return defaultAgent.Key.Name;
					  }

					  var selectedAgent = chatAgents.Keys
						  .FirstOrDefault(agent => parsedResult.Contains(agent.Name, StringComparison.OrdinalIgnoreCase));

					  if (selectedAgent != null)
					  {
						  logger.LogInformation($"Selected Agent: {selectedAgent.Name}");
						  return selectedAgent.Name;
					  }

					  logger.LogInformation("Agent not found in list. Falling back.");
					  return defaultAgent.Key.Name;
				  },
				  // The prompt variable name for the history argument.
				  HistoryVariableName = "history",
				  // Save tokens by not including the entire history in the prompt
				  HistoryReducer = new ChatHistoryTruncationReducer(3),
			  };

			var chat = new CustomAgentGroupChat(chatAgents) //new AgentGroupChat(chatAgents.Values.ToArray())
			{
				ExecutionSettings =
					new()
					{
						TerminationStrategy =
							new ApprovalTerminationStrategy()
							{
								Agents = chatAgents.Where(x => x.Key.CanTerminate).Select(x => x.Value).ToArray(),
								MaximumIterations = 20,
							},
						SelectionStrategy = selectionStrategy

					}
			};

			return chat;
		}

		private sealed class ApprovalTerminationStrategy : TerminationStrategy
		{
			private static readonly string[] sourceArray = new[] { "Done", "Finished" };

			// Terminate when the final message contains the term "approve"
			protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
			{
				var lastMessage = history[history.Count - 1];
				if (lastMessage == null) {
					return Task.FromResult(false);
				}

				if (lastMessage.Content == null)
				{
					return Task.FromResult(false);
				}

                if (sourceArray.Any(term => lastMessage.Content.Contains(term, StringComparison.OrdinalIgnoreCase)))
				{
					return Task.FromResult(true);
				}

				return Task.FromResult(false);
			}
		}
	}

	public class CustomAgentGroupChat : AgentChat
	{
		public bool IsComplete { get; set; }

		private Dictionary<IAgent, Agent> chatAgents;

		public CustomAgentGroupChat(Dictionary<IAgent, Agent> chatAgents)
		{
			this.chatAgents = chatAgents;
		}

		public AgentGroupChatSettings ExecutionSettings { get; set; } = new AgentGroupChatSettings();

		public override IReadOnlyList<Agent> Agents => chatAgents.Values.ToList();

		public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default)
		{
			for (int index = 0; index < this.ExecutionSettings.TerminationStrategy.MaximumIterations; index++)
			{
				// Identify next agent using strategy
				Agent agent = await this.SelectAgentAsync(cancellationToken).ConfigureAwait(false);

				// Invoke agent and process messages along with termination
				await foreach (var message in this.InvokeAsync(agent, cancellationToken).ConfigureAwait(false))
				{
					yield return message;
				}

				if (this.IsComplete)
				{
					break;
				}
			}
		}

		public async IAsyncEnumerable<ChatMessageContent> InvokeAsync(
			Agent agent,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (ChatMessageContent message in base.InvokeAgentAsync(agent, cancellationToken).ConfigureAwait(false))
			{
				yield return message;
			}

			var iagent = this.chatAgents.FirstOrDefault(x => x.Value == agent).Key ?? throw new InvalidOperationException($"Agent {agent.Name} not found in the chat agents.");
			await iagent.PostProcess(this.History);
			this.IsComplete = await this.ExecutionSettings.TerminationStrategy.ShouldTerminateAsync(agent, this.History, cancellationToken).ConfigureAwait(false);
		}

		public override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		private async Task<Agent> SelectAgentAsync(CancellationToken cancellationToken)
		{
			Agent agent;
			try
			{
				agent = await this.ExecutionSettings.SelectionStrategy.NextAsync(this.Agents, this.History, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				throw;
			}

			return agent;
		}
	}

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
