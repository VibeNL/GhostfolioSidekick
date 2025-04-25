using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace PortfolioViewer.WASM.AI.Agents
{
	public class PortfolioAgent : IAgent
	{
		private readonly GenericQueryAgent _queryAgent;
		private readonly IChatClient _chatAgent;

		public PortfolioAgent(GenericQueryAgent queryAgent, IChatClient chatAgent)
		{
			_queryAgent = queryAgent;
			_chatAgent = chatAgent;
		}

		public async Task<string> HandleAsync(string task, AgentContext context)
		{
			// Ask LLM how to retrieve the data (turn user task into a SQL-oriented subtask)
			var queryPrompt = $"Given this request: '{task}', describe in one sentence what SQL-based question I should ask to get relevant portfolio data.";
			var sqlTask = await _chatAgent.GetResponseAsync(queryPrompt);

			// Retrieve relevant data
			var rawData = await _queryAgent.HandleAsync(sqlTask.Text, context);

			// Ask the LLM to interpret that data
			var reasoningPrompt = $"""
            The user asked: "{task}"
            You received the following data from a portfolio database query:

            {rawData}

            Respond to the user with a useful, plain-language explanation.
        """;

			var response = await _chatAgent.GetResponseAsync(reasoningPrompt);
			if (response == null || string.IsNullOrWhiteSpace(response.Text))
			{
				return "No response from Agent.";
			}

			return response.Text.Trim();
		}
	}
}
