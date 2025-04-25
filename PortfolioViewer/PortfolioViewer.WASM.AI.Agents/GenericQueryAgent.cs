using System.Text;
using System.Text.Json;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace PortfolioViewer.WASM.AI.Agents
{
	public class GenericQueryAgent : IAgent
	{
		private readonly DatabaseContext _db;
		private readonly IChatClient _chatClient;
		private readonly ILogger<GenericQueryAgent> _logger;

		public GenericQueryAgent(DatabaseContext db, IChatClient chatClient, ILogger<GenericQueryAgent> logger)
		{
			_db = db;
			_chatClient = chatClient;
			_logger = logger;
		}

		public async Task<string> HandleAsync(string task, AgentContext context)
		{
			var schemaInfo = await GetSchemaSummaryAsync().ConfigureAwait(false);
			var prompt = $"""
Schema:
{schemaInfo}

Question:
{task}

Generate a SQL query to answer the question. Ensure the following:
- Provide only the SQL query as the output.
- Do not include any explanations, comments, or additional text.
- Format the SQL query as a single statement per line.
""";


			string sql;
			try
			{
				var response = await _chatClient.GetResponseAsync(prompt).ConfigureAwait(false);
				if (response == null || string.IsNullOrWhiteSpace(response.Text))
				{
					_logger.LogWarning("LLM response was empty or null.");
					return "No response from Agent.";
				}

				sql = response.Text.Trim();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate SQL query from LLM.");
				return $"Error generating SQL query: {ex.Message}";
			}

			// Split per line
			var sqlLines = sql.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.ToList();

			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var line in sqlLines)
				{
					var result = await _db.SqlQueryRaw<string>(line).ToListAsync();
					if (result == null || !result.Any())
					{
						_logger.LogWarning("SQL query returned no results.");
						return "No results found.";
					}

					stringBuilder.AppendLine(JsonSerializer.Serialize(result));
				}

				return stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SQL query execution failed.");
				return $"Query failed: {ex.Message}";
			}
		}

		private async Task<string> GetSchemaSummaryAsync()
		{
			try
			{
				// Retrieve all table names
				var tableNames = await _db.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'").ToListAsync();
				if (tableNames == null || !tableNames.Any())
				{
					return "No tables found in the database.";
				}

				var schemaBuilder = new StringBuilder();

				foreach (var tableName in tableNames)
				{
					schemaBuilder.AppendLine($"Table: {tableName}");

					// Retrieve column information for the current table
					var columns = await _db.SqlQueryRaw<ColumnInfo>($"PRAGMA table_info({tableName})").ToListAsync();
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
				_logger.LogError(ex, "Failed to retrieve schema summary.");
				return "Error retrieving schema summary.";
			}
		}

		// Helper class to map PRAGMA table_info results
		public class ColumnInfo
		{
			public string Name { get; set; }
			public string Type { get; set; }
		}

	}
}
