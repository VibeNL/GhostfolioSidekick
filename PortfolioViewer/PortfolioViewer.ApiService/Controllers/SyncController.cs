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
				var sqlQuery = $"SELECT * FROM {entity} ORDER BY ID LIMIT {pageSize} OFFSET {offset}";

				// Execute the raw SQL query and fetch the data into a DataTable
				using (var connection = _context.Database.GetDbConnection())
				{
					await connection.OpenAsync();
					using (var command = connection.CreateCommand())
					{
						command.CommandText = sqlQuery;
						using (var reader = await command.ExecuteReaderAsync())
						{
							var dataTable = new DataTable();
							dataTable.Load(reader);

							// Convert DataTable to a list of dictionaries for JSON serialization
							var result = dataTable.AsEnumerable()
								.Select(row => dataTable.Columns.Cast<DataColumn>()
								.ToDictionary(column => column.ColumnName, column => row[column]));

							// Return the result as JSON
							return Ok(result);
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., invalid table name)
				return BadRequest(new { Error = ex.Message });
			}
		}

		[HttpGet("{entity}/hash")]
		public async Task<IActionResult> GetEntityHash(string entity)
		{
			try
			{
				// Construct the raw SQL query to fetch all data from the specified table
				var sqlQuery = $"SELECT * FROM {entity} ORDER BY ID";

				// Execute the raw SQL query and calculate the hash incrementally
				using (var connection = _context.Database.GetDbConnection())
				{
					await connection.OpenAsync();
					using (var command = connection.CreateCommand())
					{
						command.CommandText = sqlQuery;
						using (var reader = await command.ExecuteReaderAsync())
						{
							using (var sha256 = SHA256.Create())
							{
								// Use a StringBuilder to construct the hash input incrementally
								var stringBuilder = new StringBuilder();

								while (await reader.ReadAsync())
								{
									for (int i = 0; i < reader.FieldCount; i++)
									{
										stringBuilder.Append(reader.GetValue(i)?.ToString() ?? string.Empty);
									}
								}

								// Compute the hash from the accumulated string
								var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
								var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

								// Return the hash as JSON
								return Ok(new { Hash = hashString });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., invalid table name)
				return BadRequest(new { Error = ex.Message });
			}
		}
	}
}