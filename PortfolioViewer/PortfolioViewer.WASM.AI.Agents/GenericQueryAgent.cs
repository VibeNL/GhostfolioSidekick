using System.Text;
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

		public async Task<ChatCompletionAgent> Initialize(Kernel kernel)
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

			return chatCompletionAgent;
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
	}
}
