using Microsoft.Extensions.Configuration;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using GhostfolioSidekick.PortfolioViewer.ApiService.Models;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.PortfolioViewer.Tests
{
    public class ConfigurationHelperTests : IDisposable
    {
        private readonly IConfigurationHelper _configHelper;
        private readonly List<string> _environmentVariablesToCleanup = [];
        private readonly FakeApplicationSettings _fakeApplicationSettings;

        public ConfigurationHelperTests()
        {
            // Create a test configuration
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=test.db",
                ["GoogleSearch:ApiKey"] = "test-api-key",
                ["GoogleSearch:EngineId"] = "test-engine-id",
                ["TestSetting"] = "config-value",
                ["Timeout"] = "30",
                ["TestBool"] = "true",
                ["TestDecimal"] = "123.45",
                ["TestDateTime"] = "2023-01-01T00:00:00Z",
                ["TestGuid"] = "12345678-1234-1234-1234-123456789012"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Create fake IApplicationSettings
            _fakeApplicationSettings = new FakeApplicationSettings { DatabaseFilePath = "Data Source=test.db" };

            // Use null logger for tests (logger is optional)
            _configHelper = new ConfigurationHelper(configuration, _fakeApplicationSettings);
        }

        [Fact]
        public void GetConnectionString_ReturnsValueFromApplicationSettings()
        {
            // Act
            var result = _configHelper.GetConnectionString();

            // Assert
            Assert.Equal("Data Source=test.db", result);
        }

        [Fact]
        public void GetConnectionString_ReturnsValueFromFakeApplicationSettings()
        {
            // Arrange
            _fakeApplicationSettings.DatabaseFilePath = "Data Source=env.db";

            // Act
            var result = _configHelper.GetConnectionString();

            // Assert
            Assert.Equal("Data Source=env.db", result);
        }

        [Fact]
        public void GetConfigurationValue_ReturnsStringValue()
        {
            // Act
            var result = _configHelper.GetConfigurationValue("TestSetting");

            // Assert
            Assert.Equal("config-value", result);
        }

        [Fact]
        public void GetConfigurationValue_PrefersEnvironmentVariable()
        {
            // Arrange
            SetEnvironmentVariable("TESTSETTING", "env-value");

            // Act
            var result = _configHelper.GetConfigurationValue("TestSetting");

            // Assert
            Assert.Equal("env-value", result);
        }

        [Fact]
        public void GetConfigurationValue_ThrowsForNullOrWhiteSpace()
        {
            // Act & Assert - ArgumentException.ThrowIfNullOrWhiteSpace throws different exceptions for different inputs
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationValue(""));       // Empty string -> ArgumentException
            Assert.Throws<ArgumentNullException>(() => _configHelper.GetConfigurationValue(null!));  // Null -> ArgumentNullException
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationValue("   "));     // Whitespace -> ArgumentException
        }

        [Fact]
        public void GetConfigurationValue_WithType_ConvertsToInt()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<int>("Timeout");

            // Assert
            Assert.Equal(30, result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_ConvertsToBool()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<bool>("TestBool");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_ConvertsToDecimal()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<decimal>("TestDecimal");

            // Assert
            Assert.Equal(123.45m, result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_ConvertsToDateTime()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<DateTime>("TestDateTime");

            // Assert
            Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_ConvertsToGuid()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<Guid>("TestGuid");

            // Assert
            Assert.Equal(new Guid("12345678-1234-1234-1234-123456789012"), result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_UsesDefaultValue()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<int>("NonExistentSetting", 42);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetConfigurationValue_WithType_HandlesNullableTypes()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<int?>("NonExistentSetting", null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetConfigurationSection_BindsToModel()
        {
            // Act
            var result = _configHelper.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch");

            // Assert
            Assert.Equal("test-api-key", result.ApiKey);
            Assert.Equal("test-engine-id", result.EngineId);
        }

        [Fact]
        public void GetConfigurationSection_OverridesWithEnvironmentVariables()
        {
            // Arrange
            SetEnvironmentVariable("GOOGLESEARCH_APIKEY", "env-api-key");

            // Act
            var result = _configHelper.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch");

            // Assert
            Assert.Equal("env-api-key", result.ApiKey);
            Assert.Equal("test-engine-id", result.EngineId); // This should remain from config
        }

        [Fact]
        public void GetConfigurationSection_ThrowsForNullOrWhiteSpace()
        {
            // Act & Assert - ArgumentException.ThrowIfNullOrWhiteSpace throws different exceptions for different inputs
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationSection<GoogleSearchConfiguration>(""));       // Empty string -> ArgumentException
            Assert.Throws<ArgumentNullException>(() => _configHelper.GetConfigurationSection<GoogleSearchConfiguration>(null!));  // Null -> ArgumentNullException  
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationSection<GoogleSearchConfiguration>("   "));     // Whitespace -> ArgumentException
        }

        [Fact]
        public void HasConfigurationValue_ReturnsTrueForExistingKey()
        {
            // Act
            var result = _configHelper.HasConfigurationValue("TestSetting");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasConfigurationValue_ReturnsFalseForNonExistentKey()
        {
            // Act
            var result = _configHelper.HasConfigurationValue("NonExistentSetting");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasConfigurationValue_ReturnsTrueForEnvironmentVariable()
        {
            // Arrange
            SetEnvironmentVariable("TESTENVIRONMENTVAR", "some-value");

            // Act
            var result = _configHelper.HasConfigurationValue("TestEnvironmentVar");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasConfigurationValue_ReturnsFalseForNullOrWhiteSpace()
        {
            // Act & Assert
            Assert.False(_configHelper.HasConfigurationValue(""));
            Assert.False(_configHelper.HasConfigurationValue(null!));
            Assert.False(_configHelper.HasConfigurationValue("   "));
        }

        [Fact]
        public void GetConfigurationValue_CachesEnvironmentVariables()
        {
            // Arrange
            SetEnvironmentVariable("TESTCACHE", "cached-value");

            // Act - Call multiple times
            var result1 = _configHelper.GetConfigurationValue("TestCache");
            var result2 = _configHelper.GetConfigurationValue("TestCache");

            // Assert
            Assert.Equal("cached-value", result1);
            Assert.Equal("cached-value", result2);
        }

        [Fact]
        public void GetConfigurationValue_WithType_ThrowsForNullOrWhiteSpace()
        {
            // Act & Assert - ArgumentException.ThrowIfNullOrWhiteSpace throws different exceptions for different inputs
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationValue<int>(""));       // Empty string -> ArgumentException
            Assert.Throws<ArgumentNullException>(() => _configHelper.GetConfigurationValue<int>(null!));  // Null -> ArgumentNullException
            Assert.Throws<ArgumentException>(() => _configHelper.GetConfigurationValue<int>("   "));     // Whitespace -> ArgumentException
        }

        private void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
            _environmentVariablesToCleanup.Add(name);
        }

        public void Dispose()
        {
            // Clean up environment variables
            foreach (var envVar in _environmentVariablesToCleanup)
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        private class FakeApplicationSettings : IApplicationSettings
        {
            public string FileImporterPath { get; set; } = string.Empty;
            public string DatabasePath { get; set; } = string.Empty;
            public string DatabaseFilePath { get; set; } = string.Empty;
            public string GhostfolioUrl { get; set; } = string.Empty;
            public string GhostfolioAccessToken { get; set; } = string.Empty;
            public int TrottleTimeout { get; set; }
            public int DatabaseQueryTimeoutSeconds { get; set; } = 120;
            public bool EnableDatabasePerformanceLogging { get; set; } = false;
            public ConfigurationInstance ConfigurationInstance { get; set; } = new();
            public bool AllowAdminCalls { get; set; }
        }
    }
}