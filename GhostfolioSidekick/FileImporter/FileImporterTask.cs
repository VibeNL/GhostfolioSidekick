using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask : IScheduledWork
	{
		private readonly string fileLocation;
		private readonly ILogger<FileImporterTask> logger;
		private readonly IActivitiesManager activitiesManager;
		private readonly IAccountManager accountManager;
		private readonly IMarketDataManager marketDataManager;
		private readonly IEnumerable<IFileImporter> importers;

		public int Priority => 3;

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IApplicationSettings settings,
			IActivitiesManager activitiesManager,
			IAccountManager accountManager,
			IMarketDataManager marketDataManager,
			IEnumerable<IFileImporter> importers)
		{
			ArgumentNullException.ThrowIfNull(settings);

			fileLocation = settings.FileImporterPath;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.activitiesManager = activitiesManager ?? throw new ArgumentNullException(nameof(activitiesManager));
			this.accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
			this.marketDataManager = marketDataManager ?? throw new ArgumentNullException(nameof(marketDataManager));
			this.importers = importers ?? throw new ArgumentNullException(nameof(importers));
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var directories = Directory.GetDirectories(fileLocation);

			var holdingsCollection = new HoldingsCollection(accountManager, marketDataManager);
			foreach (var directory in directories.Select(x => new DirectoryInfo(x)).OrderBy(x => x.Name))
			{
				var accountName = directory.Name;

				logger.LogInformation($"AccountName: {accountName}");

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName).Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));

					foreach (var file in files)
					{
						var importer = importers.SingleOrDefault(x => x.CanParseActivities(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");
						await importer.ParseActivities(file, holdingsCollection, accountName);
					}
				}
				catch (NoImporterAvailableException)
				{
					var sb = new StringBuilder();
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName);

					foreach (var file in files)
					{
						var importerString = string.Join(", ", importers.Select(x => $"Importer: {x.GetType().Name} CanConvert: {x.CanParseActivities(file).Result}"));
						sb.AppendLine($"{accountName} | {file} can be imported by {importerString}");
					}

					logger.LogError(sb.ToString());
				}
				catch (Exception ex)
				{
					logger.LogError($"Error {ex.Message}, {ex.StackTrace}");
					// TODO
				}
			}

			await holdingsCollection.GenerateActivities();
			activitiesManager.UpdateActivities(holdingsCollection.Holdings);

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}
	}
}
