using System;
using System.Collections.Generic;
using System.Linq;
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
		public async Task<AgentGroupChat> Initialize(Kernel kernel)
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
- Is an agent request to call another agent, Select the requested agent and ignore other rules.
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

			var chat = new AgentGroupChat(chatAgents.Values.ToArray())
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
			// Terminate when the final message contains the term "approve"
			protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
				=> Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
		}
	}

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
