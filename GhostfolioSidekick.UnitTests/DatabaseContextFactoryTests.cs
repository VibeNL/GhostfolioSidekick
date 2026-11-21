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

		public void Dispose()
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}
}
