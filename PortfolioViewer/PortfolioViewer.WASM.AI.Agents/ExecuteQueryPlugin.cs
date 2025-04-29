using System.ComponentModel;
using System.Text;
using System.Text.Json;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class ExecuteQueryPlugin(DatabaseContext databaseContext, ILogger<ExecuteQueryPlugin> logger)
	{
		[KernelFunction("execute_query")]
		[Description("Executes query")]
		public async Task<string> ExeuteQuery(string sqlStatement)
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
					var result = await databaseContext.SqlQueryRaw<string>(line).ToListAsync();
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
	}
}
