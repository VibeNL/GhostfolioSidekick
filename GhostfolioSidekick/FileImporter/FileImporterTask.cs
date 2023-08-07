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
			logger.LogInformation("Starting to do work");

			var orders = new List<Order>();

			var files = Directory.GetFiles(fileLocation, "*.*", SearchOption.AllDirectories)
				.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));

			foreach (var file in files)
			{
				logger.LogDebug($"Found file {file} to process");
				try
				{
					var accountName = new FileInfo(file).Directory.Name;

					logger.LogDebug($"AccountName: {accountName}");

					var importer = importers.Where(x => x.CanConvertOrders(file).Result).SingleOrDefault();
					if (importer == null)
					{
						throw new NotSupportedException($"File has no importer, content {File.ReadAllText(file)}");
					}

					orders.AddRange(await importer.ConvertToOrders(accountName, file));
				}
				catch (Exception ex)
				{
					logger.LogError($"Error {ex.Message}");
					// TODO
				}
			}

			await api.Write(orders.Where(x => x.Date < DateTime.Today).OrderBy(x => x.Date));
		}
	}
}
