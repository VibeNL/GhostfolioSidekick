using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask : IScheduledWork
	{
		private readonly string fileLocation;
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly IEnumerable<IFileImporter> importers;

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IConfigurationSettings settings,
			IEnumerable<IFileImporter> importers)
		{
			if (settings is null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			fileLocation = settings.FileImporterPath;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));
			this.importers = importers ?? throw new ArgumentNullException(nameof(importers));
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var accounts = new List<Model.Account>();

			var directories = Directory.GetDirectories(fileLocation);

			foreach (var directory in directories.Select(x => new DirectoryInfo(x)))
			{
				var accountName = directory.Name;
				logger.LogDebug($"AccountName: {accountName}");

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName).Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));
					var importer = importers.SingleOrDefault(x => x.CanParseActivities(files).Result) ?? throw new NoImporterAvailableException($"Directory {accountName} has no importer");
					accounts.Add(await importer.ConvertActivitiesForAccount(accountName, files));
				}
				catch (NoImporterAvailableException noImporter)
				{
					var sb = new StringBuilder();
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName);

					foreach (var file in files)
					{
						var importerString = string.Join(", ", importers.Select(x => $"Importer: {x.GetType().Name} CanConvert: {x.CanParseActivities(new[] { file }).Result}"));
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

			//await api.UpdateOrders(orders);

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}
	}
}
