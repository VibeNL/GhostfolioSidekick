using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.Strategies;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask(
		ILogger<FileImporterTask> logger,
		IApplicationSettings settings,
		IEnumerable<IFileImporter> importers,
		IEnumerable<IHoldingStrategy> strategies,
		IMemoryCache memoryCache) : IScheduledWork
	{
		private readonly string fileLocation = settings.FileImporterPath;

		public TaskPriority Priority => TaskPriority.FileImporter;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			var directories = Directory.GetDirectories(fileLocation);

			string fileHashes = CalculateHash(directories);
			var knownHash = memoryCache.TryGetValue(nameof(FileImporterTask), out string? hash) ? hash : string.Empty;
			if (fileHashes == knownHash)
			{
				logger.LogDebug("{Name} Skip to do work, no file changes detected", nameof(FileImporterTask));
				return;
			}

			logger.LogDebug("{Name} Starting to do work", nameof(FileImporterTask));

			var holdingsCollection = new HoldingsCollection(logger);
			var accountNames = new List<string>();
			foreach (var directory in directories.Select(x => new DirectoryInfo(x)).OrderBy(x => x.Name))
			{
				var accountName = directory.Name;

				logger.LogDebug("Parsing files for account: {Name}", accountName);

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName)
						.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase) ||
									x.EndsWith("pdf", StringComparison.InvariantCultureIgnoreCase));

					foreach (var file in files)
					{
						var importer = importers.SingleOrDefault(x => x.CanParse(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");

						if (importer is IActivityFileImporter activityImporter)
						{
							await activityImporter.ParseActivities(file, holdingsCollection, accountName);
						}
					}

					accountNames.Add(accountName);
				}
				catch (NoImporterAvailableException ex)
				{
					var sb = new StringBuilder();
					sb.AppendLine($"No importer available for {accountName}");

					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName);

					foreach (var file in files)
					{
						var importerString = string.Join(", ", importers.Select(x => $"Importer: {x.GetType().Name} CanConvert: {x.CanParse(file).Result}"));
						sb.AppendLine($"{accountName} | {file} can be imported by {importerString}");
					}

					logger.LogError(ex, sb.ToString());
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error {Message}", ex.Message);
				}
			}

			logger.LogDebug("Generating activities");
			await holdingsCollection.GenerateActivities();

			memoryCache.Set(nameof(FileImporterTask), fileHashes, TimeSpan.FromHours(1));

			logger.LogDebug("{Name} Done", nameof(FileImporterTask));
		}

		private static string CalculateHash(string[] directories)
		{
			var sb = new StringBuilder();

			foreach (var directory in directories.OrderBy(x => x))
			{
				var files = Directory
					.GetFiles(directory, "*.*", SearchOption.AllDirectories)
					.OrderBy(x => x.ToLowerInvariant());

				foreach (var file in files)
				{
					var fileBytes = File.ReadAllBytes(file);
					var hashBytes = SHA256.HashData(fileBytes);
					sb.Append(BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant());
				}
			}

			return sb.ToString();
		}
	}
}
