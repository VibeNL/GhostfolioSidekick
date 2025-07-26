using System.Text.Json;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
	public class PortfolioClientTests
	{
		[Fact]
		public void DeserializeData_ShouldReturnDeserializedData()
		{
			// Arrange
			var jsonData = JsonSerializer.Serialize(new List<Dictionary<string, object>>
			{
				new Dictionary<string, object> { { "Column1", "Value1" }, { "Column2", 123 } }
			});

			// Act
			var result = PortfolioClient.DeserializeData(jsonData);

			// Assert
			Assert.Single(result);
			Assert.Equal("Value1", result[0]["Column1"].ToString());
			Assert.Equal("123", result[0]["Column2"].ToString());
		}

		[Fact]
		public void DeserializeData_ShouldHandleArrayValues()
		{
			// Arrange
			var testData = new List<Dictionary<string, object>>
			{
				new Dictionary<string, object> 
				{ 
					{ "Id", "test-id" }, 
					{ "PartialSymbolIdentifiers", new[] { "AAPL", "APPLE" } } 
				}
			};
			var jsonData = JsonSerializer.Serialize(testData);

			// Act
			var result = PortfolioClient.DeserializeData(jsonData);

			// Assert
			Assert.Single(result);
			Assert.Equal("test-id", result[0]["Id"].ToString());
			Assert.Contains("AAPL", result[0]["PartialSymbolIdentifiers"].ToString()!);
			Assert.Contains("APPLE", result[0]["PartialSymbolIdentifiers"].ToString()!);
		}

		[Fact]
		public void DeserializeData_ShouldHandleEmptyInput()
		{
			// Act
			var result = PortfolioClient.DeserializeData("");

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void DeserializeData_ShouldHandleNullInput()
		{
			// Act
			var result = PortfolioClient.DeserializeData(null!);

			// Assert
			Assert.Empty(result);
		}
	}
}
