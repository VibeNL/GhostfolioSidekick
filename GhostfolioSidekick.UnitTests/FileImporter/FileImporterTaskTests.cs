using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
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
		public async Task LoadAccounts()
		{
			// Arrange
			var testImporter = new Mock<IFileImporter>();
			testImporter.Setup(x => x.CanConvertOrders(It.IsAny<IEnumerable<string>>())).ReturnsAsync(true);
			var cs = new Mock<IConfigurationSettings>();
			cs.Setup(x => x.FileImporterPath).Returns("./FileImporter/TestFiles");

			var task = new FileImporterTask(new Mock<ILogger<FileImporterTask>>().Object, api.Object, cs.Object, new[] { testImporter.Object });

			// Act
			await task.DoWork();

			// Assert
			testImporter.Verify(x => x.ConvertToOrders("DeGiro", It.Is<IEnumerable<string>>(y => y.Count() == 3)), Times.Once);
			testImporter.Verify(x => x.ConvertToOrders("ScalableCapital", It.Is<IEnumerable<string>>(y => y.Count() == 5)), Times.Once);
			testImporter.Verify(x => x.ConvertToOrders("Trading212", It.Is<IEnumerable<string>>(y => y.Count() == 4)), Times.Once);
		}
	}
}
