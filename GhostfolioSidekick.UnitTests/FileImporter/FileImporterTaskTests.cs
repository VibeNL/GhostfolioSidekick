using AutoFixture;
using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests.FileImporter
{
	public class FileImporterTaskTests
	{
		private readonly Mock<IGhostfolioAPI> api;

		public FileImporterTaskTests()
		{
			api = new Mock<IGhostfolioAPI>();
		}

		[Fact]
		public async Task DoWork_CallsConvertToActivities()
		{
			// Arrange
			var testImporter = new Mock<IFileImporter>();
			testImporter.Setup(x => x.CanParseActivities(It.IsAny<string>())).ReturnsAsync(true);
			var cs = new Mock<IApplicationSettings>();
			cs.Setup(x => x.FileImporterPath).Returns("./FileImporter/TestFiles");
			api.Setup(x => x.GetAccountByName(It.IsAny<string>())).ReturnsAsync(new Fixture().Create<Account>());

			var task = new FileImporterTask(new Mock<ILogger<FileImporterTask>>().Object, api.Object, cs.Object, new[] { testImporter.Object });

			// Act
			await task.DoWork();

			// Assert
			testImporter.Verify(x => x.ConvertToActivities(It.IsAny<string>(), It.IsAny<Balance>()), Times.AtLeastOnce);
		}
	}
}
