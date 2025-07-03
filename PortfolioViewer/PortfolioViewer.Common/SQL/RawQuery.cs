using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.Common.SQL
{
	public static class RawQuery
	{
		public static async Task<List<Dictionary<string, object>>> ReadTable(DatabaseContext databaseContext, string entity, int page, int pageSize)
		{
			// Calculate the offset for pagination
			var offset = (page - 1) * pageSize;

			// Construct the raw SQL query with pagination
			entity = await ValidateTableNameAsync(databaseContext, entity);
			var sqlQuery = $"SELECT * FROM {entity} ORDER BY 1 LIMIT @pageSize OFFSET @offset";

			// Execute the raw SQL query and fetch the data into a DataTable
			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = sqlQuery;

			var pageSizeParam = command.CreateParameter();
			pageSizeParam.ParameterName = "@pageSize";
			pageSizeParam.Value = pageSize;
			command.Parameters.Add(pageSizeParam);

			var offsetParam = command.CreateParameter();
			offsetParam.ParameterName = "@offset";
			offsetParam.Value = offset;
			command.Parameters.Add(offsetParam);

			using var reader = await command.ExecuteReaderAsync();

			// Read data directly from the reader into a list of dictionaries
			var result = new List<Dictionary<string, object>>();
			while (await reader.ReadAsync())
			{
				var row = new Dictionary<string, object>();
				for (var i = 0; i < reader.FieldCount; i++)
				{
					row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
				}
				result.Add(row);
			}

			return result;
		}

		public static async Task<int> GetTableCount(DatabaseContext context, string entity)
		{
			entity = await ValidateTableNameAsync(context, entity);
			var sqlQuery = $"SELECT COUNT(*) FROM {entity}";
			
			using var connection = context.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = sqlQuery;
			
			var result = await command.ExecuteScalarAsync();
			return Convert.ToInt32(result);
		}

		private static async Task<string> ValidateTableNameAsync(DatabaseContext context, string entity)
		{
			// Ensure the table name is valid and does not contain invalid characters
			if (string.IsNullOrWhiteSpace(entity) || entity.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
			{
				throw new ArgumentException("Invalid table name.");
			}

			// Check if the table exists in the database
			var query = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{entity}'";
			
			using var connection = context.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = query;
			
			var result = await command.ExecuteScalarAsync();
			var tableExists = Convert.ToInt32(result) > 0;

			if (!tableExists)
			{
				throw new ArgumentException($"Table '{entity}' does not exist in the database.");
			}

			return entity;
		}
	}
}
