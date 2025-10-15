using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Data.Common;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Services
{
	[Collection("NonParallel")]
	public class SyncGrpcServiceTests
	{
		private sealed class TestDb : IDisposable
		{
			public DatabaseContext Context { get; }
			public SqliteConnection Connection { get; }
			private readonly string _path;

			public TestDb()
			{
				_path = Path.Combine(Path.GetTempPath(), $"testdb_{Guid.NewGuid():N}.db");
				Connection = new SqliteConnection($"Data Source={_path}");
				Connection.Open();

				var options = new DbContextOptionsBuilder<DatabaseContext>()
					.UseSqlite(Connection)
					.Options;

				Context = new DatabaseContext(options);
				Context.Database.EnsureCreated();
			}

			public void Dispose()
			{
				try
				{
					Context?.Dispose();
				}
				catch
				{
					// Ignore
				}

				try
				{
					Connection?.Close();
					Connection?.Dispose();
				}
				catch
				{
					// Ignore
				}

				try
				{
					if (File.Exists(_path))
					{
						File.Delete(_path);
					}
				}
				catch
				{
					// Ignore
				}
			}
		}

		private class TestServerStreamWriter<T> : IServerStreamWriter<T>
		{
			public List<T> Written { get; } = [];
			public WriteOptions? WriteOptions { get; set; }
			public Task WriteAsync(T message)
			{
				Written.Add(message);
				return Task.CompletedTask;
			}
		}

		private class FakeServerCallContext : ServerCallContext
		{
			protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
			protected override string MethodCore => "test";
			protected override string HostCore => "localhost";
			protected override string PeerCore => "peer";
			protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
			protected override Metadata RequestHeadersCore => [];
			protected override CancellationToken CancellationTokenCore => CancellationToken.None;
			protected override Metadata ResponseTrailersCore => [];
			protected override Status StatusCore { get; set; }
			protected override WriteOptions? WriteOptionsCore { get; set; }
			protected override AuthContext AuthContextCore => new("", []);
			protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();
		}

		[Fact]
		public async Task GetTableNames_ReturnsTablesAndCounts()
		{
			using var db = new TestDb();
			var ctx = db.Context;

			// create tables and insert rows using the same connection
			using (var cmd = db.Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE test_table (id INTEGER PRIMARY KEY, created_date TEXT, name TEXT);";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "CREATE TABLE other_table (id INTEGER PRIMARY KEY, value TEXT);";
				await cmd.ExecuteNonQueryAsync();

				cmd.CommandText = "INSERT INTO test_table (created_date, name) VALUES ('2020-01-01', 'a'), ('2021-01-01', 'b');";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "INSERT INTO other_table (value) VALUES ('x');";
				await cmd.ExecuteNonQueryAsync();
			}

			var logger = new Mock<ILogger<SyncGrpcService> >().Object;
			var service = new SyncGrpcService(ctx, logger);
			var response = await service.GetTableNames(new GetTableNamesRequest(), new FakeServerCallContext());

			Assert.Contains("test_table", response.TableNames);
			Assert.Contains("other_table", response.TableNames);

			// total rows should correspond to the insert counts, order is the same as TableNames
			var dict = response.TableNames.Zip(response.TotalRows, (t, c) => (t, c)).ToDictionary(x => x.t, x => x.c);
			Assert.Equal(2, dict["test_table"]);
			Assert.Equal(1, dict["other_table"]);
		}

		[Fact]
		public async Task GetLatestDates_ReturnsMaxDateForTablesWithDateColumn()
		{
			using var db = new TestDb();
			var ctx = db.Context;

			using (var cmd = db.Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE with_date (id INTEGER PRIMARY KEY, mydate TEXT);";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "INSERT INTO with_date (mydate) VALUES ('2020-01-01'), ('2022-05-03');";
				await cmd.ExecuteNonQueryAsync();

				cmd.CommandText = "CREATE TABLE without_date (id INTEGER PRIMARY KEY, val TEXT);";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "INSERT INTO without_date (val) VALUES ('v');";
				await cmd.ExecuteNonQueryAsync();
			}

			var logger = new Mock<ILogger<SyncGrpcService> >().Object;
			var service = new SyncGrpcService(ctx, logger);
			var response = await service.GetLatestDates(new GetLatestDatesRequest(), new FakeServerCallContext());

			Assert.True(response.LatestDates.ContainsKey("with_date"));
			Assert.Equal("2022-05-03", response.LatestDates["with_date"]);
			Assert.False(response.LatestDates.ContainsKey("without_date"));
		}

		[Fact]
		public async Task GetEntityData_WritesPagedResultsAndHasMore()
		{
			using var db = new TestDb();
			var ctx = db.Context;

			using (var cmd = db.Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, created_date TEXT, name TEXT);";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "INSERT INTO items (created_date, name) VALUES ('2020-01-01','a'), ('2021-01-01','b'), ('2022-01-01','c');";
				await cmd.ExecuteNonQueryAsync();
			}

			var logger = new Mock<ILogger<SyncGrpcService> >().Object;
			var service = new SyncGrpcService(ctx, logger);

			var writer = new TestServerStreamWriter<GetEntityDataResponse>();
			var req = new GetEntityDataRequest { Entity = "items", Page = 1, PageSize = 2 };
			await service.GetEntityData(req, writer, new FakeServerCallContext());

			// one response should have been written
			Assert.Single(writer.Written);
			var resp = writer.Written.First();
			Assert.Equal(1, resp.CurrentPage);
			Assert.True(resp.HasMore);
			Assert.Equal(2, resp.Records.Count);

			// second page should contain remaining record and HasMore false
			var writer2 = new TestServerStreamWriter<GetEntityDataResponse>();
			var req2 = new GetEntityDataRequest { Entity = "items", Page = 2, PageSize = 2 };
			await service.GetEntityData(req2, writer2, new FakeServerCallContext());
			Assert.Single(writer2.Written);
			Assert.False(writer2.Written.First().HasMore);
			Assert.Single(writer2.Written.First().Records);
		}

		[Fact]
		public async Task GetEntityDataSince_FiltersByDate()
		{
			using var db = new TestDb();
			var ctx = db.Context;

			using (var cmd = db.Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE events (id INTEGER PRIMARY KEY, event_date TEXT, name TEXT);";
				await cmd.ExecuteNonQueryAsync();
				cmd.CommandText = "INSERT INTO events (event_date, name) VALUES ('2020-01-01','a'), ('2021-06-01','b'), ('2022-07-01','c');";
				await cmd.ExecuteNonQueryAsync();
			}

			var logger = new Mock<ILogger<SyncGrpcService> >().Object;
			var service = new SyncGrpcService(ctx, logger);

			var writer = new TestServerStreamWriter<GetEntityDataResponse>();
			var req = new GetEntityDataSinceRequest { Entity = "events", Page = 1, PageSize = 10, SinceDate = "2021-01-01" };
			await service.GetEntityDataSince(req, writer, new FakeServerCallContext());

			Assert.Single(writer.Written);
			var resp = writer.Written.First();
			// should include two records (2021-06-01 and 2022-07-01)
			Assert.Equal(2, resp.Records.Count);
			var names = resp.Records.Select(r => r.Fields["name"]).ToList();
			Assert.Contains("b", names);
			Assert.Contains("c", names);
		}

		[Fact]
		public async Task GetEntityData_InvalidPage_ThrowsRpcException()
		{
			using var db = new TestDb();
			var ctx = db.Context;

			using (var cmd = db.Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT);";
				await cmd.ExecuteNonQueryAsync();
			}

			var logger = new Mock<ILogger<SyncGrpcService> >().Object;
			var service = new SyncGrpcService(ctx, logger);

			var writer = new TestServerStreamWriter<GetEntityDataResponse>();
			var req = new GetEntityDataRequest { Entity = "t", Page = 0, PageSize = 10 };

			await Assert.ThrowsAsync<RpcException>(() => service.GetEntityData(req, writer, new FakeServerCallContext()));
		}
	}
}
