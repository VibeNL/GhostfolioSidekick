using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.Configuration.UnitTests
{
	public class ApplicationSettingsTests : IDisposable
	{
		private readonly string configFile;

		public ApplicationSettingsTests()
		{
			const string configFileContent = "{\"settings\" : {\"dataprovider.preference.order\": \"COINGECKO,YAHOO\"} }";
			configFile = Path.GetTempFileName();
			File.WriteAllText(configFile, configFileContent);
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", configFile);
		}

		[Fact]
		public void Constructor_ParsesConfigurationFile()
		{
			// Arrange
			// Act
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Assert
			settings.ConfigurationInstance.Should().NotBeNull();
			settings.ConfigurationInstance.Settings.DataProviderPreference.Should().Be("COINGECKO,YAHOO");
		}

		[Fact]
		public void FileImporterPath_ReturnsEnvironmentVariableValue()
		{
			// Arrange
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "some_path");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.FileImporterPath;

			// Assert
			Assert.Equal("some_path", result);

			// Clean up
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", null);
		}

		[Fact]
		public void GhostfolioAccessToken_ReturnsEnvironmentVariableValue()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", "some_token");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.GhostfolioAccessToken;

			// Assert
			Assert.Equal("some_token", result);

			// Clean up
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", null);
		}

		[Fact]
		public void GhostfolioUrl_ReturnsNormalizedUrl()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", "http://example.com/");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.GhostfolioUrl;

			// Assert
			Assert.Equal("http://example.com", result);

			// Clean up
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", null);
		}

		[Fact]
		public void ConfigurationInstance_ReturnsNonNullInstance()
		{
			// Arrange
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.ConfigurationInstance;

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public void AllowAdminCalls_DefaultValueIsTrue()
		{
			// Arrange
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.AllowAdminCalls;

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void ConfigurationInstance_NoEnvironmentSettings_ReturnsNonNullInstance()
		{
			// Arrange
			Dispose();
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.ConfigurationInstance;

			// Assert
			Assert.NotNull(result);
		}

		[Fact]
		public void GetFileImporterPath_ReturnsCorrectPath()
		{
			// Arrange
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", "test_path");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.FileImporterPath;

			// Assert
			result.Should().Be("test_path");

			// Clean up
			Environment.SetEnvironmentVariable("FILEIMPORTER_PATH", null);
		}

		[Fact]
		public void GetGhostfolioAccessToken_ReturnsCorrectToken()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", "test_token");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.GhostfolioAccessToken;

			// Assert
			result.Should().Be("test_token");

			// Clean up
			Environment.SetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN", null);
		}

		[Fact]
		public void GetGhostfolioUrl_ReturnsCorrectUrl()
		{
			// Arrange
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", "http://test.com/");
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.GhostfolioUrl;

			// Assert
			result.Should().Be("http://test.com");

			// Clean up
			Environment.SetEnvironmentVariable("GHOSTFOLIO_URL", null);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Clean up
			File.Delete(configFile);
			Environment.SetEnvironmentVariable("CONFIGURATIONFILE_PATH", null);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
