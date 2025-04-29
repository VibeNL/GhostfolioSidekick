using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class GenericQueryAgent(DatabaseContext db, string name, ILogger<GenericQueryAgent> logger) : IAgent
	{
		public bool CanTerminate => false;

		public string Name => nameof(GenericQueryAgent);

		public bool InitialAgent => false;

		public object? Description => "Can query the portfolio and market data";

		public async Task<Agent> Initialize(Kernel kernel)
		{
			var schemaInfo = await GetSchemaSummaryAsync().ConfigureAwait(false);

			var chatCompletionAgent = new ChatCompletionAgent
			{
				Instructions = $"""
								Schema:
								{schemaInfo}
								
								Generate a SQL query to answer the question. Ensure the following:
								- Provide only the SQL query as the output.
								- Do not include any explanations, comments, or additional text.
								- Format the SQL query as a single statement per line.
								""",
				Name = name,
				Kernel = kernel
			};

			var wrapperAgent = new WrapperAgent(chatCompletionAgent);

			return chatCompletionAgent;
		}

		private async Task<string> ExeuteQuery(string sqlStatement)
		{
			// Split per line
			var sqlLines = sqlStatement.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.ToList();

			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var line in sqlLines)
				{
					var result = await db.SqlQueryRaw<string>(line).ToListAsync();
					if (result == null || !result.Any())
					{
						logger.LogWarning("SQL query returned no results.");
						return "No results found.";
					}

					stringBuilder.AppendLine(JsonSerializer.Serialize(result));
				}

				return stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SQL query execution failed.");
				return $"Query failed: {ex.Message}";
			}
		}

		private async Task<string> GetSchemaSummaryAsync()
		{
			try
			{
				// Retrieve all table names
				var tableNames = await db.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'").ToListAsync();
				if (tableNames == null || !tableNames.Any())
				{
					return "No tables found in the database.";
				}

				var schemaBuilder = new StringBuilder();

				foreach (var tableName in tableNames)
				{
					schemaBuilder.AppendLine($"Table: {tableName}");

					// Retrieve column information for the current table
					var columns = await db.SqlQueryRaw<ColumnInfo>($"PRAGMA table_info({tableName})").ToListAsync();
					if (columns != null && columns.Any())
					{
						foreach (var column in columns)
						{
							schemaBuilder.AppendLine($"  Column: {column.Name}, Type: {column.Type}");
						}
					}
					else
					{
						schemaBuilder.AppendLine("  No columns found.");
					}

					schemaBuilder.AppendLine(); // Add a blank line between tables
				}

				return schemaBuilder.ToString();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to retrieve schema summary.");
				return "Error retrieving schema summary.";
			}
		}

		// Helper class to map PRAGMA table_info results
		public class ColumnInfo
		{
			public string Name { get; set; }
			public string Type { get; set; }
		}

		private class WrapperAgent(Agent inner) : Agent
		{
			public override IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> InvokeAsync(ICollection<ChatMessageContent> messages, AgentThread? thread = null, AgentInvokeOptions? options = null, CancellationToken cancellationToken = default)
			{
				return inner.InvokeAsync(messages, thread, options, cancellationToken);
			}

			public override IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> InvokeStreamingAsync(ICollection<ChatMessageContent> messages, AgentThread? thread = null, AgentInvokeOptions? options = null, CancellationToken cancellationToken = default)
			{
				return inner.InvokeStreamingAsync(messages, thread, options, cancellationToken);
			}

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			protected override async Task<AgentChannel> CreateChannelAsync(CancellationToken cancellationToken)

			{
				throw new NotImplementedException();
			}

			protected override IEnumerable<string> GetChannelKeys()
			{
				throw new NotImplementedException();
			}

			protected override Task<AgentChannel> RestoreChannelAsync(string channelState, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
		}
	}
}
