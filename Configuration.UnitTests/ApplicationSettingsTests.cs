using Shouldly;
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
			settings.ConfigurationInstance.ShouldNotBeNull();
			settings.ConfigurationInstance.Settings.DataProviderPreference.ShouldBe("COINGECKO,YAHOO");
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
			result.ShouldBe("some_path");

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
			result.ShouldBe("some_token");

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
			result.ShouldBe("http://example.com");

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
			result.ShouldNotBeNull();
		}

		[Fact]
		public void AllowAdminCalls_DefaultValueIsTrue()
		{
			// Arrange
			var settings = new ApplicationSettings(new Mock<ILogger<ApplicationSettings>>().Object);

			// Act
			var result = settings.AllowAdminCalls;

			// Assert
			result.ShouldBeTrue();
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
			result.ShouldNotBeNull();
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
