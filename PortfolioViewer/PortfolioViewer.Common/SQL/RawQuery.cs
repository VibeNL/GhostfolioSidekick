using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.Common.SQL
{
	public static class RawQuery
	{
		public static async Task<List<Dictionary<string, object>>> ReadTable(DatabaseContext databaseContext, string entity, int page, int pageSize)
		{
			return await ReadTable(databaseContext, entity, page, pageSize, null, null, null);
		}

		public static async Task<List<Dictionary<string, object>>> ReadTable(DatabaseContext databaseContext, string entity, int page, int pageSize, Dictionary<string, string>? columnFilters)
		{
			return await ReadTable(databaseContext, entity, page, pageSize, columnFilters, null, null);
		}

		public static async Task<List<Dictionary<string, object>>> ReadTable(DatabaseContext databaseContext, string entity, int page, int pageSize, Dictionary<string, string>? columnFilters, string? sortColumn, string? sortDirection)
		{
			// Calculate the offset for pagination
			var offset = (page - 1) * pageSize;

			// Construct the raw SQL query with pagination and filtering
			entity = await ValidateTableNameAsync(databaseContext, entity);
			var sqlQuery = $"SELECT * FROM {entity}";

			// Add WHERE clause if filters are provided
			var parameters = new List<(string name, object value)>();
			if (columnFilters != null && columnFilters.Any())
			{
				var whereConditions = new List<string>();
				var paramIndex = 0;
				
				foreach (var filter in columnFilters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
				{
					// Validate column name to prevent SQL injection
					var columnName = await ValidateColumnNameAsync(databaseContext, entity, filter.Key);
					var paramName = $"@filter{paramIndex}";
					whereConditions.Add($"{columnName} LIKE {paramName}");
					parameters.Add((paramName, $"%{filter.Value}%"));
					paramIndex++;
				}
				
				if (whereConditions.Any())
				{
					sqlQuery += " WHERE " + string.Join(" AND ", whereConditions);
				}
			}

			// Add ORDER BY clause
			if (!string.IsNullOrWhiteSpace(sortColumn))
			{
				// Validate the sort column name
				var validatedSortColumn = await ValidateColumnNameAsync(databaseContext, entity, sortColumn);
				var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
				sqlQuery += $" ORDER BY {validatedSortColumn} {direction}";
			}
			else
			{
				// Default sort by first column
				sqlQuery += " ORDER BY 1";
			}

			sqlQuery += " LIMIT @pageSize OFFSET @offset";

			// Execute the raw SQL query
			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = sqlQuery;

			// Add filter parameters
			foreach (var (name, value) in parameters)
			{
				var param = command.CreateParameter();
				param.ParameterName = name;
				param.Value = value;
				command.Parameters.Add(param);
			}

			// Add pagination parameters
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
			return await GetTableCount(context, entity, null);
		}

		public static async Task<int> GetTableCount(DatabaseContext context, string entity, Dictionary<string, string>? columnFilters)
		{
			entity = await ValidateTableNameAsync(context, entity);
			var sqlQuery = $"SELECT COUNT(*) FROM {entity}";
			
			// Add WHERE clause if filters are provided
			var parameters = new List<(string name, object value)>();
			if (columnFilters != null && columnFilters.Any())
			{
				var whereConditions = new List<string>();
				var paramIndex = 0;
				
				foreach (var filter in columnFilters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
				{
					// Validate column name to prevent SQL injection
					var columnName = await ValidateColumnNameAsync(context, entity, filter.Key);
					var paramName = $"@filter{paramIndex}";
					whereConditions.Add($"{columnName} LIKE {paramName}");
					parameters.Add((paramName, $"%{filter.Value}%"));
					paramIndex++;
				}
				
				if (whereConditions.Any())
				{
					sqlQuery += " WHERE " + string.Join(" AND ", whereConditions);
				}
			}
			
			using var connection = context.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = sqlQuery;
			
			// Add filter parameters
			foreach (var (name, value) in parameters)
			{
				var param = command.CreateParameter();
				param.ParameterName = name;
				param.Value = value;
				command.Parameters.Add(param);
			}
			
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

		private static async Task<string> ValidateColumnNameAsync(DatabaseContext context, string entity, string columnName)
		{
			// Ensure the column name is valid and does not contain invalid characters
			if (string.IsNullOrWhiteSpace(columnName) || columnName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
			{
				throw new ArgumentException("Invalid column name.");
			}

			// Check if the column exists in the table
			var query = $"PRAGMA table_info({entity})";
			
			using var connection = context.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = query;
			
			using var reader = await command.ExecuteReaderAsync();
			var columnExists = false;
			while (await reader.ReadAsync())
			{
				var name = reader.GetString(1); // Column name is at index 1 in PRAGMA table_info
				if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
				{
					columnExists = true;
					break;
				}
			}

			if (!columnExists)
			{
				throw new ArgumentException($"Column '{columnName}' does not exist in table '{entity}'.");
			}

			return columnName;
		}
	}
}
