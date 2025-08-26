using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;

namespace GhostfolioSidekick.Activities
{
	public class FileImporterTask(
		ILogger<FileImporterTask> logger,
		IApplicationSettings settings,
		IEnumerable<IFileImporter> importers,
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IMemoryCache memoryCache) : IScheduledWork
	{
		private readonly string fileLocation = settings.FileImporterPath;

		public TaskPriority Priority => TaskPriority.FileImporter;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

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

			using var databaseContext = databaseContextFactory.CreateDbContext();
			var activityManager = new ActivityManager(await databaseContext.Accounts.ToListAsync());
			var accountNames = new List<string>();
			await ParseFiles(logger, importers, directories, activityManager, accountNames);

			logger.LogDebug("Generating activities");
			var activities = await activityManager.GenerateActivities();

			// write to the dababase.
			await StoreAll(databaseContext, activities);

			memoryCache.Set(nameof(FileImporterTask), fileHashes, TimeSpan.FromHours(1));

			logger.LogDebug("{Name} Done", nameof(FileImporterTask));
		}

		private static async Task ParseFiles(ILogger<FileImporterTask> logger, IEnumerable<IFileImporter> importers, string[] directories, ActivityManager activityManager, List<string> accountNames)
		{
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
						var importer = importers.SingleOrDefault(x => x.CanParse(file).Result);

						if (importer is null && file.EndsWith("csv"))
						{
							throw new NoImporterAvailableException($"No importer available for {file}");
						}
						else if (importer is null)
						{
							logger.LogWarning("No importer available for {File}", file);
							continue;
						}

						if (importer is IActivityFileImporter activityImporter)
						{
							await activityImporter.ParseActivities(file, activityManager, accountName);
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

		[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Complex database operations with async calls require explicit loops")]
		public static async Task StoreAll(DatabaseContext databaseContext, IEnumerable<Activity> activities)
		{
			// Change to database partialsymbolidentifiers
			var existingPartialSymbolIdentifiers = await databaseContext.PartialSymbolIdentifiers.ToListAsync();

			foreach (var activity in activities.OfType<IActivityWithPartialIdentifier>())
			{
				// If missing from the database, add it
				foreach (var partialSymbolIdentifier in activity.PartialSymbolIdentifiers)
				{
					if (!existingPartialSymbolIdentifiers.Any(x => x == partialSymbolIdentifier))
					{
						await databaseContext.PartialSymbolIdentifiers.AddAsync(partialSymbolIdentifier);
						existingPartialSymbolIdentifiers.Add(partialSymbolIdentifier);
					}
				}

				activity.PartialSymbolIdentifiers = [.. activity.PartialSymbolIdentifiers.Select(x => existingPartialSymbolIdentifiers.FirstOrDefault(y => y == x) ?? x)];
			}


			// Deduplicate entities
			var existingActivities = await databaseContext.Activities.ToListAsync();
			var existingTransactionIds = existingActivities.Select(x => x.TransactionId).ToList();
			var newTransactionIds = activities.Select(x => x.TransactionId).ToList();

			// Delete activities that are not in the new list
			foreach (var deletedTransaction in existingTransactionIds.Except(newTransactionIds))
			{
				databaseContext.Activities.RemoveRange(existingActivities.Where(x => x.TransactionId == deletedTransaction));
			}

			// Add activities that are not in the existing list
			foreach (var addedTransaction in newTransactionIds.Except(existingTransactionIds))
			{
				await databaseContext.Activities.AddRangeAsync(activities.Where(x => x.TransactionId == addedTransaction));
			}

			// Update activities that are in both lists
			foreach (var updatedTransaction in existingTransactionIds.Intersect(newTransactionIds))
			{
				var existingActivity = existingActivities.Where(x => x.TransactionId == updatedTransaction).OrderBy(x => x.SortingPriority).ThenBy(x => x.Description);
				var newActivity = activities.Where(x => x.TransactionId == updatedTransaction).OrderBy(x => x.SortingPriority).ThenBy(x => x.Description);

				var compareLogic = new CompareLogic()
				{
					Config = new ComparisonConfig
					{
						MaxDifferences = int.MaxValue,
						IgnoreObjectTypes = true,
						MembersToIgnore = [
							nameof(Activity.Id), 
							nameof(BuySellActivity.AdjustedQuantity),
							nameof(BuySellActivity.AdjustedUnitPrice),
							nameof(BuySellActivity.AdjustedUnitPriceSource),
							nameof(BuySellActivityFee.ActivityId)]
					}
				};
				ComparisonResult result = compareLogic.Compare(existingActivity, newActivity);

				if (!result.AreEqual)
				{
					databaseContext.Activities.RemoveRange(existingActivity);
					await databaseContext.Activities.AddRangeAsync(newActivity);
				}
			}

			await databaseContext.SaveChangesAsync();
		}
	}
}
