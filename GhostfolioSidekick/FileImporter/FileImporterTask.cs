using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask : IScheduledWork
	{
		private readonly string? fileLocation;
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly IEnumerable<IFileImporter> importers;

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IEnumerable<IFileImporter> importers)
		{
			fileLocation = Environment.GetEnvironmentVariable("FileImporterPath");
			this.logger = logger;
			this.api = api;
			this.importers = importers;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var orders = new List<Order>();

			var files = Directory.GetFiles(fileLocation, "*.*", SearchOption.AllDirectories)
				.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));

			foreach (var fileGroup in files.GroupBy(x => new FileInfo(x).Directory.Name))
			{
				logger.LogDebug($"Found file {fileGroup.Key} to process");
				try
				{
					var accountName = fileGroup.Key;

					logger.LogDebug($"AccountName: {accountName}");

					var importer = importers.SingleOrDefault(x => x.CanConvertOrders(fileGroup).Result);
					if (importer == null)
					{
						throw new NotSupportedException($"Filegroup {fileGroup.Key} has no importer");
					}

					orders.AddRange(await importer.ConvertToOrders(accountName, fileGroup));
				}
				catch (Exception ex)
				{
					logger.LogError($"Error {ex.Message}");
					// TODO
				}
			}

			await api.Write(orders.Where(x => x.Date < DateTime.Today).OrderBy(x => x.Date));

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}
	}
}
