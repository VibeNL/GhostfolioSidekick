using Microsoft.Extensions.Configuration;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using GhostfolioSidekick.PortfolioViewer.ApiService.Models;

namespace GhostfolioSidekick.PortfolioViewer.Tests
{
    public class ConfigurationHelperTests : IDisposable
    {
        private readonly IConfigurationHelper _configHelper;
        private readonly List<string> _environmentVariablesToCleanup = new();

        public ConfigurationHelperTests()
        {
            // Create a test configuration
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=test.db",
                ["GoogleSearch:ApiKey"] = "test-api-key",
                ["GoogleSearch:EngineId"] = "test-engine-id",
                ["TestSetting"] = "config-value",
                ["Timeout"] = "30"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _configHelper = new ConfigurationHelper(configuration);
        }

        [Fact]
        public void GetConnectionString_ReturnsValueFromConfiguration()
        {
            // Act
            var result = _configHelper.GetConnectionString("DefaultConnection");

            // Assert
            Assert.Equal("Data Source=test.db", result);
        }

        [Fact]
        public void GetConnectionString_PrefersEnvironmentVariable()
        {
            // Arrange
            SetEnvironmentVariable("CONNECTIONSTRING_DEFAULT", "Data Source=env.db");

            // Act
            var result = _configHelper.GetConnectionString("DefaultConnection");

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
        public void GetConfigurationValue_WithType_ConvertsToInt()
        {
            // Act
            var result = _configHelper.GetConfigurationValue<int>("Timeout");

            // Assert
            Assert.Equal(30, result);
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
    }
}