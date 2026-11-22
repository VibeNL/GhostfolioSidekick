using GhostfolioSidekick.Configuration;
using Moq;

namespace GhostfolioSidekick.UnitTests
{
	public class DatabaseContextFactoryTests : IDisposable
	{
		private readonly string tempDirectory;

		public DatabaseContextFactoryTests()
		{
			tempDirectory = Path.Combine(Path.GetTempPath(), $"DatabaseContextFactoryTests_{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDirectory);
		}

		[Fact]
		public void CreateDatabaseOpenErrorMessage_MissingDirectory_ExplainsMissingPath()
		{
			string dbPath = Path.Combine(tempDirectory, "nested", "ghostfolio.db");

			string message = DatabaseContextFactory.CreateDatabaseOpenErrorMessage(dbPath);

			Assert.Contains("does not exist", message);
			Assert.Contains(Path.Combine(tempDirectory, "nested"), message);
		}

		[Fact]
		public void CreateDatabaseOpenErrorMessage_ExistingDirectory_SuggestsOwnershipFix()
		{
			string dbPath = Path.Combine(tempDirectory, "ghostfolio.db");

			string message = DatabaseContextFactory.CreateDatabaseOpenErrorMessage(dbPath);

			Assert.Contains("chown 1654:1654", message);
			Assert.Contains(dbPath, message);
		}

		[Fact]
		public void CreateDbContext_MissingDirectory_ThrowsInvalidOperationException()
		{
			string dbPath = Path.Combine(tempDirectory, "missing", "ghostfolio.db");
			var settingsMock = new Mock<IApplicationSettings>();
			settingsMock.Setup(x => x.DatabaseFilePath).Returns(dbPath);
			var serviceProviderMock = new Mock<IServiceProvider>();
			serviceProviderMock.Setup(x => x.GetService(typeof(IApplicationSettings))).Returns(settingsMock.Object);

			DatabaseContextFactory factory = new(serviceProviderMock.Object);

			InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext());
			Assert.Contains("does not exist", exception.Message);
		}

		public void Dispose()
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}
}
