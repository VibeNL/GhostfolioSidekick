using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Services
{
    public class ConfigurationHelperTests
    {
        [Fact]
        public void GetConnectionString_ReturnsDatabaseFilePath()
        {
            var configuration = new ConfigurationBuilder().Build();
            var appSettings = new Mock<IApplicationSettings>();
            appSettings.Setup(x => x.DatabaseFilePath).Returns("C:\\temp\\db.sqlite");
            var logger = new Mock<ILogger<ConfigurationHelper>>();

            var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

            var result = sut.GetConnectionString();

            Assert.Equal("C:\\temp\\db.sqlite", result);
        }

        [Fact]
        public void GetConfigurationValue_UsesEnvironmentVariable_WhenPresent()
        {
            var key = "Sample:Setting:Env";
            var envName = key.Replace(":", "_").Replace(".", "_").ToUpperInvariant();
            try
            {
                Environment.SetEnvironmentVariable(envName, "from-env");

                var configuration = new ConfigurationBuilder().Build();
                var appSettings = new Mock<IApplicationSettings>();
                var logger = new Mock<ILogger<ConfigurationHelper>>();

                var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

                var value = sut.GetConfigurationValue(key);

                Assert.Equal("from-env", value);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, null);
            }
        }

        [Fact]
        public void GetConfigurationValue_UsesConfiguration_WhenEnvironmentNotPresent()
        {
            var key = "App:Value";
            var mem = new Dictionary<string, string?>
            {
                { key, "from-config" }
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(mem).Build();
            var appSettings = new Mock<IApplicationSettings>();
            var logger = new Mock<ILogger<ConfigurationHelper>>();

            var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

            var value = sut.GetConfigurationValue(key);

            Assert.Equal("from-config", value);
        }

        [Fact]
        public void GetConfigurationValue_Generic_ParsesInt_FromEnvironment()
        {
            var key = "Test:IntValue";
            var envName = key.Replace(":", "_").Replace(".", "_").ToUpperInvariant();
            try
            {
                Environment.SetEnvironmentVariable(envName, "123");

                var configuration = new ConfigurationBuilder().Build();
                var appSettings = new Mock<IApplicationSettings>();
                var logger = new Mock<ILogger<ConfigurationHelper>>();

                var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

                var value = sut.GetConfigurationValue<int>(key);

                Assert.Equal(123, value);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, null);
            }
        }

        [Fact]
        public void GetConfigurationValue_Generic_ReturnsDefault_WhenProvided()
        {
            var configuration = new ConfigurationBuilder().Build();
            var appSettings = new Mock<IApplicationSettings>();
            var logger = new Mock<ILogger<ConfigurationHelper>>();

            var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

            var value = sut.GetConfigurationValue<int>("Missing:Key", 42);

            Assert.Equal(42, value);
        }

        [Fact]
        public void GetConfigurationValue_Generic_Throws_WhenMissingAndNoDefaultForNonNullable()
        {
            var configuration = new ConfigurationBuilder().Build();
            var appSettings = new Mock<IApplicationSettings>();
            var logger = new Mock<ILogger<ConfigurationHelper>>();

            var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

            Assert.Throws<InvalidOperationException>(() => sut.GetConfigurationValue<int>("Missing:Key"));
        }

        [Fact]
        public void HasConfigurationValue_ReturnsTrue_ForEnvironmentVariable()
        {
            var key = "Has:Env:Flag";
            var envName = key.Replace(":", "_").Replace(".", "_").ToUpperInvariant();
            try
            {
                Environment.SetEnvironmentVariable(envName, "1");

                var configuration = new ConfigurationBuilder().Build();
                var appSettings = new Mock<IApplicationSettings>();
                var logger = new Mock<ILogger<ConfigurationHelper>>();

                var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

                Assert.True(sut.HasConfigurationValue(key));
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, null);
            }
        }

        [Fact]
        public void HasConfigurationValue_ReturnsTrue_ForConfiguration()
        {
            var key = "Exists:InConfig";
            var mem = new Dictionary<string, string?> { { key, "yes" } };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(mem).Build();
            var appSettings = new Mock<IApplicationSettings>();
            var logger = new Mock<ILogger<ConfigurationHelper>>();

            var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

            Assert.True(sut.HasConfigurationValue(key));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major", "S1144", Justification = "Used by ConfigurationBinder via reflection")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "<Pending>")]
		private class Person
        {
			public string? Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public void GetConfigurationSection_BindsAndOverridesWithEnvironmentVariables()
        {
            var sectionName = "MySection";
            var mem = new Dictionary<string, string?>
            {
                { "MySection:Name", "Bob" },
                { "MySection:Age", "30" }
            };

            var envName = ($"{sectionName.ToUpperInvariant()}_AGE");
            try
            {
                // override age via env var
                Environment.SetEnvironmentVariable(envName, "40");

                var configuration = new ConfigurationBuilder().AddInMemoryCollection(mem).Build();
                var appSettings = new Mock<IApplicationSettings>();
                var logger = new Mock<ILogger<ConfigurationHelper>>();

                var sut = new ConfigurationHelper(configuration, appSettings.Object, logger.Object);

                var person = sut.GetConfigurationSection<Person>(sectionName);

                Assert.Equal("Bob", person.Name);
                Assert.Equal(40, person.Age);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, null);
            }
        }
    }
}
