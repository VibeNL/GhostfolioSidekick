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
			IConfigurationSettings settings,
			IEnumerable<IFileImporter> importers)
		{
			fileLocation = settings.FileImporterPath;
			this.logger = logger;
			this.api = api;
			this.importers = importers;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var orders = new List<Order>();

			var directories = Directory.GetDirectories(fileLocation);

			foreach (var directory in directories.Select(x => new DirectoryInfo(x)))
			{
				var accountName = directory.Name;
				logger.LogDebug($"AccountName: {accountName}");

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName);
					var importer = importers.SingleOrDefault(x => x.CanConvertOrders(files).Result);
					if (importer == null)
					{
						throw new NotSupportedException($"Directory {accountName} has no importer");
					}

					orders.AddRange(await importer.ConvertToOrders(accountName, files));
				}
				catch (Exception ex)
				{
					logger.LogError($"Error {ex.Message}, {ex.StackTrace}");
					// TODO
				}
			}

			await api.Write(orders.Where(x => x.Date < DateTime.Today).OrderBy(x => x.Date));

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}
	}
}
