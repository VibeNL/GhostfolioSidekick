using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class GenericQueryAgent(DatabaseContext db, string name, ILogger<GenericQueryAgent> logger) : IAgent
	{
		public bool CanTerminate => false;

		public string Name => nameof(GenericQueryAgent);

		public bool InitialAgent => false;

		public string Description => "Can query the portfolio and market data";

		public async Task<Agent> Initialize(Kernel kernel)
		{
			var schemaInfo = await GetSchemaSummaryAsync().ConfigureAwait(false);

			var chatCompletionAgent = new ChatCompletionAgent
			{
				Instructions = $"""
								Schema:
								{schemaInfo}
								
								You are a SQL generator. Given a question, your task is to output **only one valid SQL statement**, and nothing else.
								You will be provided with a schema of the database, and you must use it to generate the SQL statement.

								You must follow these rules exactly:
								- Output a single SQL statement only.
								- Do not include comments, explanations, or any natural language.
								- Do not prepend or append labels like 'Answer:' or 'Explanation:'.
								- Do not use code blocks, markdown, or quotes.
								- Do not repeat the query.
								""",
				Name = name,
				Kernel = kernel
			};

			return chatCompletionAgent;
		}

		public async Task<bool> PostProcess(ChatHistory history)
		{
			if (history.Count == 0)
			{
				return false;
			}

			var lastMessage = history[history.Count - 1];
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			if (lastMessage is not ChatMessageContent chatMessage || chatMessage.AuthorName != this.Name)
			{
				return false;
			}
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

			var sqlStatement = chatMessage.Content;
			if (string.IsNullOrWhiteSpace(sqlStatement))
			{
				return false;
			}

			// Execute the SQL statement
			var result = await ExecuteQuery(CleanModelOutput(sqlStatement));

			if (!string.IsNullOrWhiteSpace(result))
			{
				history.AddAssistantMessage($"Results from {Name}: {result}");
			}
			else
			{
				history.AddAssistantMessage($"Results from {Name}: No results found.");
			}

			return false;
		}

		string CleanModelOutput(string raw)
		{
			var firstLine = raw
				.Split('\n')
				.FirstOrDefault(l => l.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
									 l.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase));

			return firstLine?.TrimEnd(';') + ";";
		}

		private async Task<string> ExecuteQuery(string sqlStatement)
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
			var sqlLines = sqlStatement.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
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
