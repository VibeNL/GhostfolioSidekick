using Microsoft.AspNetCore.Mvc;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Data;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SyncController : ControllerBase
	{
		private readonly DatabaseContext _context;

		public SyncController(DatabaseContext context)
		{
			_context = context;
		}

		[HttpGet("{entity}")]
		public async Task<IActionResult> GetEntityData(string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
		{
			if (page <= 0 || pageSize <= 0)
			{
				return BadRequest(new { Error = "Page and pageSize must be greater than 0." });
			}

			try
			{
				// Calculate the offset for pagination
				var offset = (page - 1) * pageSize;

				// Construct the raw SQL query with pagination
				ValidateTableName(_context, entity);
				var sqlQuery = $"SELECT * FROM {entity} ORDER BY ID LIMIT @pageSize OFFSET @offset";

				// Execute the raw SQL query and fetch the data into a DataTable
				using var connection = _context.Database.GetDbConnection();
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
				var dataTable = new DataTable();
				dataTable.Load(reader);

				// Convert DataTable to a list of dictionaries for JSON serialization
				var result = dataTable.AsEnumerable()
					.Select(row => dataTable.Columns.Cast<DataColumn>()
					.ToDictionary(column => column.ColumnName, column => row[column]));

				// Return the result as JSON
				return Ok(result);
			}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., invalid table name)
				return BadRequest(new { Error = ex.Message });
			}
		}

		private void ValidateTableName(DatabaseContext context, string entity)
		{
			// Ensure the table name is valid and does not contain invalid characters
			if (string.IsNullOrWhiteSpace(entity) || entity.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
			{
				throw new ArgumentException("Invalid table name.");
			}

			// Check if the table exists in the database
			var query = $"SELECT COUNT(*) as VALUE FROM sqlite_master WHERE type='table' AND name='{entity}'";
			var tableExists = context.Database.SqlQueryRaw<int>(query).FirstOrDefault() > 0;

			if (!tableExists)
			{
				throw new ArgumentException($"Table '{entity}' does not exist in the database.");
			}
		}
	}
}