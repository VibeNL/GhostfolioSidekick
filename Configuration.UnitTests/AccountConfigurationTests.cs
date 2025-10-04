using System.Text.Json;

namespace GhostfolioSidekick.Configuration.UnitTests
{
	public class AccountConfigurationTests
	{
		[Fact]
		public void AccountConfiguration_ShouldSetDefaultValues()
		{
			// Arrange & Act
			var config = new AccountConfiguration
			{
				Name = "Test Account",
				Currency = "USD"
			};

			// Assert
			Assert.True(config.SyncActivities);
			Assert.True(config.SyncBalance);
		}

		[Fact]
		public void AccountConfiguration_ShouldSerializeAndDeserializeCorrectly()
		{
			// Arrange
			var config = new AccountConfiguration
			{
				Name = "De Giro",
				Currency = "EUR",
				Platform = "De Giro",
				SyncActivities = true,
				SyncBalance = false,
				Comment = "Test comment"
			};

			// Act
			var json = JsonSerializer.Serialize(config);
			var deserializedConfig = JsonSerializer.Deserialize<AccountConfiguration>(json);

			// Assert
			Assert.NotNull(deserializedConfig);
			Assert.Equal(config.Name, deserializedConfig.Name);
			Assert.Equal(config.Currency, deserializedConfig.Currency);
			Assert.Equal(config.Platform, deserializedConfig.Platform);
			Assert.Equal(config.SyncActivities, deserializedConfig.SyncActivities);
			Assert.Equal(config.SyncBalance, deserializedConfig.SyncBalance);
			Assert.Equal(config.Comment, deserializedConfig.Comment);
		}

		[Fact]
		public void AccountConfiguration_ShouldDeserializeWithMissingProperties()
		{
			// Arrange
			var json = """
			{
				"name": "De Giro",
				"currency": "EUR",
				"platform": "De Giro"
			}
			""";

			// Act
			var config = JsonSerializer.Deserialize<AccountConfiguration>(json);

			// Assert
			Assert.NotNull(config);
			Assert.Equal("De Giro", config.Name);
			Assert.Equal("EUR", config.Currency);
			Assert.Equal("De Giro", config.Platform);
			Assert.True(config.SyncActivities); // Should default to true
			Assert.True(config.SyncBalance); // Should default to true
		}

		[Fact]
		public void AccountConfiguration_ShouldDeserializeWithExplicitValues()
		{
			// Arrange
			var json = """
			{
				"name": "De Giro",
				"currency": "EUR",
				"platform": "De Giro",
				"sync-activities": false,
				"sync-balance": true,
				"comment": "Test comment"
			}
			""";

			// Act
			var config = JsonSerializer.Deserialize<AccountConfiguration>(json);

			// Assert
			Assert.NotNull(config);
			Assert.Equal("De Giro", config.Name);
			Assert.Equal("EUR", config.Currency);
			Assert.Equal("De Giro", config.Platform);
			Assert.False(config.SyncActivities);
			Assert.True(config.SyncBalance);
			Assert.Equal("Test comment", config.Comment);
		}
	}
}