using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask : IScheduledWork
	{
		private readonly string fileLocation;
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly IEnumerable<IFileImporter> importers;

		public int Priority => 2;

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IApplicationSettings settings,
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

			var directories = Directory.GetDirectories(fileLocation);

			foreach (var directory in directories.Select(x => new DirectoryInfo(x)).OrderBy(x => x.Name))
			{
				var accountName = directory.Name;
				logger.LogInformation($"AccountName: {accountName}");

				try
				{
					var account = await api.GetAccountByName(accountName);

					if (account == null)
					{
						logger.LogError($"Error Account {accountName} not found");
						continue;
					}

					var files = directory
						.GetFiles("*.*", SearchOption.AllDirectories)
						.Select(x => x.FullName)
						.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase));


					var activities = new List<Activity>();
					foreach (var file in files)
					{
						var importer = importers.SingleOrDefault(x => x.CanParseActivities(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");
						activities.AddRange(await importer.ConvertToActivities(file, account.Balance.Currency));
					}

					account.ReplaceActivities(activities);
					await api.UpdateAccount(account);
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

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}
	}
}
