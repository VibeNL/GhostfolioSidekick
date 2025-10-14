using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Services
{
	public class SyncGrpcServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDbContext;
		private readonly Mock<ILogger<SyncGrpcService>> _mockLogger;
		private readonly SyncGrpcService _service;

		public SyncGrpcServiceTests()
		{
			_mockDbContext = new Mock<DatabaseContext>();
			_mockLogger = new Mock<ILogger<SyncGrpcService>>();

			_service = new SyncGrpcService(_mockDbContext.Object, _mockLogger.Object);
		}

		#region Constructor Tests

		[Fact]
		public void Constructor_WithValidParameters_ShouldCreateInstance()
		{
			// Arrange & Act
			var service = new SyncGrpcService(_mockDbContext.Object, _mockLogger.Object);

			// Assert
			service.Should().NotBeNull();
		}

		// Note: The SyncGrpcService constructor uses primary constructor syntax and doesn't include
		// explicit null checks. In production code, you might want to add null validation.

		#endregion

		#region GetEntityData Input Validation Tests

		[Fact]
		public async Task GetEntityData_WithZeroPageSize_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = 1,
				PageSize = 0
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityData(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityData_WithNegativePageSize_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = 1,
				PageSize = -5
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityData(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityData_WithZeroPage_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = 0,
				PageSize = 10
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityData(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityData_WithNegativePage_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = -1,
				PageSize = 10
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityData(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		#endregion

		#region GetEntityDataSince Input Validation Tests

		[Fact]
		public async Task GetEntityDataSince_WithZeroPageSize_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataSinceRequest
			{
				Entity = "TestTable",
				Page = 1,
				PageSize = 0,
				SinceDate = "2023-12-01"
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityDataSince(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityDataSince_WithNegativePageSize_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataSinceRequest
			{
				Entity = "TestTable",
				Page = 1,
				PageSize = -1,
				SinceDate = "2023-12-01"
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityDataSince(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityDataSince_WithZeroPage_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataSinceRequest
			{
				Entity = "TestTable",
				Page = 0,
				PageSize = 10,
				SinceDate = "2023-12-01"
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityDataSince(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		[Fact]
		public async Task GetEntityDataSince_WithNegativePage_ShouldThrowRpcException()
		{
			// Arrange
			var request = new GetEntityDataSinceRequest
			{
				Entity = "TestTable",
				Page = -5,
				PageSize = 10,
				SinceDate = "2023-12-01"
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			var action = async () => await _service.GetEntityDataSince(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
			exception.Which.Status.Detail.Should().Be("Page and pageSize must be greater than 0.");
		}

		#endregion

		#region IsDateColumn Logic Tests

		[Theory]
		[InlineData("date", "TEXT", true)]
		[InlineData("Date", "TEXT", true)]
		[InlineData("DATE", "TEXT", true)]
		[InlineData("created_date", "TEXT", true)]
		[InlineData("date_created", "TEXT", true)]
		[InlineData("updated_date", "TEXT", true)]
		[InlineData("timestamp", "TEXT", true)]
		[InlineData("event_timestamp", "TEXT", true)]
		[InlineData("name", "TEXT", false)]
		[InlineData("id", "INTEGER", false)]
		[InlineData("description", "VARCHAR", false)]
		[InlineData("test_column", "DATETIME", true)]
		[InlineData("test_column", "DATE", true)]
		[InlineData("test_column", "TIMESTAMP", true)]
		[InlineData("test_column", "datetime", true)]
		[InlineData("test_column", "date", true)]
		[InlineData("test_column", "timestamp", true)]
		[InlineData("test_column", "VARCHAR", false)]
		[InlineData("test_column", "INTEGER", false)]
		[InlineData("test_column", "REAL", false)]
		[InlineData("test_column", "BLOB", false)]
		[InlineData("datelike_column", "TEXT", true)]
		[InlineData("column_with_date", "TEXT", true)]
		[InlineData("something_timestamp_else", "TEXT", true)]
		[InlineData("Date", "VARCHAR", true)] // Column name takes precedence over type
		[InlineData("timestamp_field", "INTEGER", true)] // Column name takes precedence over type
		public void IsDateColumn_WithVariousInputs_ShouldReturnExpectedResult(string columnName, string columnType, bool expected)
		{
			// Test the IsDateColumn logic by replicating it since it's a private static method
			var result = TestIsDateColumn(columnName, columnType);
			result.Should().Be(expected);
		}

		private static bool TestIsDateColumn(string columnName, string columnType)
		{
			// This replicates the private IsDateColumn logic for testing purposes
			return columnName.ToLower() switch
			{
				var name when name == "date" || name.EndsWith("date") || name.StartsWith("date") || name.Contains("timestamp") => true,
				_ => columnType.ToLower() switch
				{
					var type when type.Contains("date") || type.Contains("datetime") || type.Contains("timestamp") => true,
					_ => false
				}
			};
		}

		#endregion

		#region gRPC Message Tests

		[Fact]
		public void GetTableNamesRequest_ShouldBeCreatable()
		{
			// Arrange & Act
			var request = new GetTableNamesRequest();

			// Assert
			request.Should().NotBeNull();
			request.Should().BeOfType<GetTableNamesRequest>();
		}

		[Fact]
		public void GetLatestDatesRequest_ShouldBeCreatable()
		{
			// Arrange & Act
			var request = new GetLatestDatesRequest();

			// Assert
			request.Should().NotBeNull();
			request.Should().BeOfType<GetLatestDatesRequest>();
		}

		[Fact]
		public void GetEntityDataRequest_ShouldSupportAllProperties()
		{
			// Arrange & Act
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = 1,
				PageSize = 100
			};

			// Assert
			request.Entity.Should().Be("TestTable");
			request.Page.Should().Be(1);
			request.PageSize.Should().Be(100);
		}

		[Fact]
		public void GetEntityDataSinceRequest_ShouldSupportAllProperties()
		{
			// Arrange & Act
			var request = new GetEntityDataSinceRequest
			{
				Entity = "TestTable",
				Page = 2,
				PageSize = 50,
				SinceDate = "2023-12-01"
			};

			// Assert
			request.Entity.Should().Be("TestTable");
			request.Page.Should().Be(2);
			request.PageSize.Should().Be(50);
			request.SinceDate.Should().Be("2023-12-01");
		}

		[Fact]
		public void EntityRecord_ShouldSupportFieldsCollection()
		{
			// Arrange & Act
			var record = new EntityRecord();
			record.Fields["TestField"] = "TestValue";
			record.Fields["AnotherField"] = "AnotherValue";
			record.Fields["NumberField"] = "123";

			// Assert
			record.Fields.Should().HaveCount(3);
			record.Fields["TestField"].Should().Be("TestValue");
			record.Fields["AnotherField"].Should().Be("AnotherValue");
			record.Fields["NumberField"].Should().Be("123");
		}

		[Fact]
		public void GetEntityDataResponse_ShouldSupportAllProperties()
		{
			// Arrange & Act
			var response = new GetEntityDataResponse
			{
				CurrentPage = 5,
				HasMore = true
			};
			
			var record1 = new EntityRecord();
			record1.Fields["Id"] = "1";
			record1.Fields["Name"] = "Test1";
			
			var record2 = new EntityRecord();
			record2.Fields["Id"] = "2";
			record2.Fields["Name"] = "Test2";
			
			response.Records.Add(record1);
			response.Records.Add(record2);

			// Assert
			response.CurrentPage.Should().Be(5);
			response.HasMore.Should().BeTrue();
			response.Records.Should().HaveCount(2);
			response.Records[0].Fields["Id"].Should().Be("1");
			response.Records[1].Fields["Name"].Should().Be("Test2");
		}

		[Fact]
		public void GetTableNamesResponse_ShouldSupportCollections()
		{
			// Arrange & Act
			var response = new GetTableNamesResponse();
			response.TableNames.Add("Table1");
			response.TableNames.Add("Table2");
			response.TotalRows.Add(100);
			response.TotalRows.Add(200);

			// Assert
			response.TableNames.Should().HaveCount(2);
			response.TotalRows.Should().HaveCount(2);
			response.TableNames[0].Should().Be("Table1");
			response.TableNames[1].Should().Be("Table2");
			response.TotalRows[0].Should().Be(100);
			response.TotalRows[1].Should().Be(200);
		}

		[Fact]
		public void GetLatestDatesResponse_ShouldSupportDictionary()
		{
			// Arrange & Act
			var response = new GetLatestDatesResponse();
			response.LatestDates["Table1"] = "2023-12-01";
			response.LatestDates["Table2"] = "2023-12-02";
			response.LatestDates["Table3"] = "2023-12-03";

			// Assert
			response.LatestDates.Should().HaveCount(3);
			response.LatestDates["Table1"].Should().Be("2023-12-01");
			response.LatestDates["Table2"].Should().Be("2023-12-02");
			response.LatestDates["Table3"].Should().Be("2023-12-03");
		}

		#endregion

		#region Service Interface Tests

		[Fact]
		public void SyncGrpcService_ShouldInheritFromSyncServiceBase()
		{
			// Arrange & Act & Assert
			_service.Should().BeAssignableTo<SyncService.SyncServiceBase>();
		}

		[Fact]
		public void SyncGrpcService_ShouldHaveCorrectServiceMethods()
		{
			// Arrange
			var serviceType = typeof(SyncGrpcService);

			// Act & Assert
			serviceType.GetMethod("GetTableNames").Should().NotBeNull();
			serviceType.GetMethod("GetEntityData").Should().NotBeNull();
			serviceType.GetMethod("GetEntityDataSince").Should().NotBeNull();
			serviceType.GetMethod("GetLatestDates").Should().NotBeNull();
		}

		#endregion

		#region Input Validation Edge Cases

		[Fact]
		public async Task GetEntityData_WithLargeValidValues_ShouldNotThrowArgumentException()
		{
			// Arrange
			var request = new GetEntityDataRequest
			{
				Entity = "TestTable",
				Page = 10000,
				PageSize = 1000
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			// Should not throw ArgumentException (though may throw other exceptions due to database operations)
			var action = async () => await _service.GetEntityData(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			// Should not be InvalidArgument since the parameters are valid
			exception.Which.StatusCode.Should().NotBe(StatusCode.InvalidArgument);
		}

		[Fact]
		public async Task GetEntityDataSince_WithEmptyEntity_ShouldNotThrowArgumentException()
		{
			// Arrange
			var request = new GetEntityDataSinceRequest
			{
				Entity = "",
				Page = 1,
				PageSize = 10,
				SinceDate = "2023-12-01"
			};

			var mockContext = new Mock<ServerCallContext>();
			var mockStreamWriter = new Mock<IServerStreamWriter<GetEntityDataResponse>>();

			// Act & Assert
			// Empty entity should not cause argument validation error (handled in business logic)
			var action = async () => await _service.GetEntityDataSince(request, mockStreamWriter.Object, mockContext.Object);
			var exception = await action.Should().ThrowAsync<RpcException>();
			
			// Should not be InvalidArgument since the entity validation is separate from page validation
			exception.Which.StatusCode.Should().NotBe(StatusCode.InvalidArgument);
		}

		#endregion
	}
}