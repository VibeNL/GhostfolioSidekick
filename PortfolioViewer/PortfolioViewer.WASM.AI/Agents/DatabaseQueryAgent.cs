using System.Text;
using System.Xml.Linq;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
    internal class DatabaseQueryAgent(IWebChatClient webChatClient, DatabaseContext db, ILogger<DatabaseQueryAgent> logger) : IAgent
    {
        public string Name => nameof(DatabaseQueryAgent);

        public string Description => "Executes queries against the portfolio database and returns results.";

		public bool IsDefault => false;

		public async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(IEnumerable<ChatMessage> messages, AgentContext context)
        {
			var schemaInfo = await GetSchemaSummaryAsync().ConfigureAwait(false);
			var prompt = $"""
								Schema:
								{schemaInfo}
								
								Queries to execute in natural language:
								{string.Join(Environment.NewLine, context.Memory.Select(m => $"{m.Role}: {m.Text}"))}
								
								You are a SQL generator. Given a question, your task is to output **only one valid SQL statement**, and nothing else.
								You will be provided with a schema of the database, and you must use it to generate the SQL statement.
								You must follow these rules exactly:
								- Output a single SQL statement only.
								- Do not include comments, explanations, or any natural language.
								- Do not prepend or append labels like 'Answer:' or 'Explanation:'.
								- Do not use code blocks, markdown, or quotes.
								- Do not repeat the query.
								""";

			var llmResponse = await webChatClient.GetResponseAsync(prompt);

			if (llmResponse.Text == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Tool, "No response from Database.");
				yield break;
			}

			// Get the SQL statement from the LLM response
			var result = await ExecuteQuery(llmResponse.Text);

			context.Memory.Add(new ChatMessage(ChatRole.Tool, result) { AuthorName = Name });

			// yield the response
			yield return new ChatResponseUpdate(ChatRole.Assistant, result);

		}

		private async Task<string> ExecuteQuery(string sqlStatement)
		{
			// Split per line
			var sqlLines = sqlStatement.Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.ToList();

			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var line in sqlLines)
				{
					var result = await ExecuteQueryInternal(line);
					if (result == null || !result.Any())
					{
						logger.LogWarning("SQL query returned no results.");
						return "No results found.";
					}

					stringBuilder.AppendLine(result);
				}

				return stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SQL query execution failed.");
				return $"Query failed: {ex.Message}";
			}
		}

		private async Task<string> ExecuteQueryInternal(string sqlStatement)
		{
			// Split per line and clean up the SQL statement
			var sqlLines = sqlStatement.Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.ToList();

			try
			{
				StringBuilder stringBuilder = new StringBuilder();

				foreach (var line in sqlLines)
				{
					var dynamicResults = await db.ExecuteDynamicQuery(line);

					if (dynamicResults == null || !dynamicResults.Any())
					{
						logger.LogWarning("SQL query returned no results.");
						stringBuilder.AppendLine("No results found.");
					}
					else
					{
						// Convert the dynamic result to a more readable format (e.g., JSON or CSV)
						stringBuilder.AppendLine($"Results from the query:");
						stringBuilder.AppendLine(FormatResultsAsTable(dynamicResults));
					}
				}

				return stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "SQL query execution failed.");
				return $"Query failed: {ex.Message}";
			}
		}

		private string FormatResultsAsTable(List<Dictionary<string, object>> results)
		{
			StringBuilder tableBuilder = new StringBuilder();

			// Print headers
			var headers = results.FirstOrDefault()?.Keys.ToList();
			if (headers != null && headers.Any())
			{
				tableBuilder.AppendLine(string.Join(" | ", headers));
				tableBuilder.AppendLine(new string('-', headers.Count * 12)); // Simple separator line
			}

			// Print each row
			foreach (var row in results)
			{
				var rowValues = row.Values.Select(val => val?.ToString() ?? "NULL").ToList();
				tableBuilder.AppendLine(string.Join(" | ", rowValues));
			}

			return tableBuilder.ToString();
		}

		private async Task<string> GetSchemaSummaryAsync()
		{
			try
			{
				var tableNames = await db.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'").ToListAsync();
				if (tableNames == null || !tableNames.Any())
				{
					return "No tables found in the database.";
				}

				var schemaBuilder = new StringBuilder();

				foreach (var tableName in tableNames)
				{
					var columns = await db.SqlQueryRaw<ColumnInfo>($"PRAGMA table_info({tableName})").ToListAsync();
					if (columns != null && columns.Any())
					{
						schemaBuilder.AppendLine($"CREATE TABLE {tableName} (");

						var columnDefinitions = columns.Select(col => $"    {col.Name} {col.Type}");
						schemaBuilder.AppendLine(string.Join(",\n", columnDefinitions));

						schemaBuilder.AppendLine(");");
						schemaBuilder.AppendLine(); // Extra newline for readability
					}
					else
					{
						schemaBuilder.AppendLine($"-- No columns found for table {tableName}");
					}
				}

				return schemaBuilder.ToString() + "\n" + await GetDiscriminatorSummaryAsync();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to retrieve schema summary.");
				return "Error retrieving schema summary.";
			}
		}

		private async Task<string> GetDiscriminatorSummaryAsync()
		{
			try
			{
				var tableNames = await db.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'").ToListAsync();
				if (tableNames == null)
				{
					return string.Empty;
				}

				var summaryBuilder = new StringBuilder();

				foreach (var tableName in tableNames)
				{
					var columns = await db.SqlQueryRaw<ColumnInfo>($"PRAGMA table_info({tableName})").ToListAsync();
					if (columns.Any(c => c.Name.Equals("Discriminator", StringComparison.OrdinalIgnoreCase)))
					{
						var values = await db.SqlQueryRaw<string>($"SELECT DISTINCT Discriminator FROM {tableName}").ToListAsync();

						if (values != null && values.Any())
						{
							summaryBuilder.AppendLine($"Note: The `{tableName}` table uses a discriminator column `Discriminator` with the following values:");
							foreach (var value in values)
							{
								summaryBuilder.AppendLine($"- \"{value}\"");
							}
							summaryBuilder.AppendLine();
						}
					}
				}

				return summaryBuilder.ToString();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to retrieve discriminator summary.");
				return string.Empty;
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
