using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.Tests
{
	public class RawQueryTests : IDisposable
	{
		private readonly string dbFilePath;
		private readonly SqliteConnection connection;
		private readonly DatabaseContext context;

		public RawQueryTests()
		{
			dbFilePath = Path.Combine(Path.GetTempPath(), $"testdb_{Guid.NewGuid():N}.db");
			connection = new SqliteConnection($"Data Source={dbFilePath}");
			connection.Open();

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;

			context = new DatabaseContext(options);
		}

		public void Dispose()
		{
			context.Dispose();
			connection.Dispose();
			try
			{
				File.Delete(dbFilePath);
			}
			catch
			{
				// Ignore any errors during cleanup
			}

			GC.SuppressFinalize(this);
		}

		// Use the same connection that EF Core's DatabaseContext uses to ensure created tables
		// are visible to RawQuery which opens the DbConnection from the context.
		private async Task CreateTableAsync(string tableName, string columns, params (string sql, object?[]? parameters)[] inserts)
		{
			var conn = context.Database.GetDbConnection();
			await context.Database.OpenConnectionAsync();

			using var cmd = conn.CreateCommand();
			cmd.CommandText = $"CREATE TABLE {tableName} ({columns});";
			await cmd.ExecuteNonQueryAsync();

			foreach (var (sql, parameters) in inserts)
			{
				using var icmd = conn.CreateCommand();
				icmd.CommandText = sql;
				if (parameters != null)
				{
					for (int i = 0; i < parameters.Length; i++)
					{
						var p = icmd.CreateParameter();
						p.ParameterName = $"@p{i}";
						p.Value = parameters[i] ?? DBNull.Value;
						icmd.Parameters.Add(p);
					}
				}
				await icmd.ExecuteNonQueryAsync();
			}
		}

		[Fact]
		public async Task ReadTable_Pagination_ReturnsExpectedRows()
		{
			// Arrange
			await CreateTableAsync("TestTable1", "Id INTEGER PRIMARY KEY, Name TEXT",
				("INSERT INTO TestTable1 (Name) VALUES (@p0);", new object?[] { "A" }),
				("INSERT INTO TestTable1 (Name) VALUES (@p0);", new object?[] { "B" }),
				("INSERT INTO TestTable1 (Name) VALUES (@p0);", new object?[] { "C" })
			);

			// Act
			var page1 = await RawQuery.ReadTable(context, "TestTable1", 1, 2);
			var page2 = await RawQuery.ReadTable(context, "TestTable1", 2, 2);

			// Assert
			Assert.Equal(2, page1.Count);
			Assert.Single(page2);
			Assert.Equal("A", page1[0]["Name"] as string);
		}

		[Fact]
		public async Task ReadTable_FilterAndGetTableCount_Works()
		{
			// Arrange
			await CreateTableAsync("People", "Id INTEGER PRIMARY KEY, Name TEXT",
				("INSERT INTO People (Name) VALUES (@p0);", new object?[] { "Alice" }),
				("INSERT INTO People (Name) VALUES (@p0);", new object?[] { "Bob" }),
				("INSERT INTO People (Name) VALUES (@p0);", new object?[] { "Alicia" })
			);

			var filters = new Dictionary<string, string> { { "Name", "Ali" } };

			// Act
			var rows = await RawQuery.ReadTable(context, "People", 1, 10, filters);
			var count = await RawQuery.GetTableCount(context, "People", filters);

			// Assert
			Assert.Equal(2, rows.Count);
			Assert.Equal(2, count);
			Assert.Contains(rows.Select(r => r["Name"] as string), s => s == "Alice");
			Assert.Contains(rows.Select(r => r["Name"] as string), s => s == "Alicia");
		}

		[Fact]
		public async Task ReadTable_SortDirection_Works()
		{
			// Arrange
			await CreateTableAsync("Numbers", "Id INTEGER PRIMARY KEY, Value INTEGER",
				("INSERT INTO Numbers (Value) VALUES (@p0);", new object?[] { 10 }),
				("INSERT INTO Numbers (Value) VALUES (@p0);", new object?[] { 20 }),
				("INSERT INTO Numbers (Value) VALUES (@p0);", new object?[] { 30 })
			);

			// Act
			var asc = await RawQuery.ReadTable(context, "Numbers", 1, 10, null, "Value", "asc");
			var desc = await RawQuery.ReadTable(context, "Numbers", 1, 10, null, "Value", "desc");

			// Assert
			Assert.Equal(3, asc.Count);
			Assert.Equal(3, desc.Count);
			Assert.Equal(10L, Convert.ToInt64(asc[0]["Value"]));
			Assert.Equal(30L, Convert.ToInt64(desc[0]["Value"]));
		}

		[Fact]
		public async Task GetTableCount_NoFilters_ReturnsTotal()
		{
			// Arrange
			await CreateTableAsync("Items", "Id INTEGER PRIMARY KEY, Name TEXT",
				("INSERT INTO Items (Name) VALUES (@p0);", new object?[] { "X" }),
				("INSERT INTO Items (Name) VALUES (@p0);", new object?[] { "Y" })
			);

			// Act
			var count = await RawQuery.GetTableCount(context, "Items");

			// Assert
			Assert.Equal(2, count);
		}

		[Fact]
		public async Task ReadTable_NonExistingTable_ThrowsArgumentException()
		{
			// Arrange / Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () => await RawQuery.ReadTable(context, "NoSuchTable", 1, 10));
		}

		[Fact]
		public async Task ReadTable_NonExistingColumn_ThrowsArgumentException()
		{
			// Arrange
			await CreateTableAsync("Simple", "Id INTEGER PRIMARY KEY, Name TEXT",
				("INSERT INTO Simple (Name) VALUES (@p0);", new object?[] { "V" })
			);

			var filters = new Dictionary<string, string> { { "UnknownColumn", "x" } };

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(async () => await RawQuery.ReadTable(context, "Simple", 1, 10, filters));
		}
	}
}
