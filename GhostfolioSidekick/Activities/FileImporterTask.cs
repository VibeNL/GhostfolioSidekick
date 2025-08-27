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

		[SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Linear logic")]
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
			await UpdatePartialSymbolIdentifiers(databaseContext, activities);
			await SyncActivities(databaseContext, activities);
			await databaseContext.SaveChangesAsync();
		}

		private static async Task UpdatePartialSymbolIdentifiers(DatabaseContext databaseContext, IEnumerable<Activity> activities)
		{
			var existingPartialSymbolIdentifiers = await databaseContext.PartialSymbolIdentifiers.ToListAsync();

			foreach (var activity in activities.OfType<IActivityWithPartialIdentifier>())
			{
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
		}

		private static async Task SyncActivities(DatabaseContext databaseContext, IEnumerable<Activity> activities)
		{
			var existingActivities = await databaseContext.Activities.ToListAsync();
			
			var existingTransactionKeys = GetTransactionKeys(existingActivities);
			var newTransactionKeys = GetTransactionKeys(activities);

			DeleteRemovedActivities(databaseContext, existingActivities, existingTransactionKeys, newTransactionKeys);
			await AddNewActivities(databaseContext, activities, existingTransactionKeys, newTransactionKeys);
			await UpdateExistingActivities(databaseContext, existingActivities, activities, existingTransactionKeys, newTransactionKeys);
		}

		private static List<(string TransactionId, int AccountId)> GetTransactionKeys(IEnumerable<Activity> activities)
		{
			return [.. activities
				.Select(x => (x.TransactionId, x.Account.Id))
				.Distinct()];
		}

		private static void DeleteRemovedActivities(
			DatabaseContext databaseContext, 
			List<Activity> existingActivities,
			List<(string TransactionId, int AccountId)> existingKeys,
			List<(string TransactionId, int AccountId)> newKeys)
		{
			var deletedKeys = existingKeys.Except(newKeys);
			foreach (var (TransactionId, AccountId) in deletedKeys)
			{
				var activitiesToDelete = existingActivities.Where(x => 
					x.TransactionId == TransactionId && 
					x.Account.Id == AccountId);
				databaseContext.Activities.RemoveRange(activitiesToDelete);
			}
		}

		private static async Task AddNewActivities(
			DatabaseContext databaseContext,
			IEnumerable<Activity> activities,
			List<(string TransactionId, int AccountId)> existingKeys,
			List<(string TransactionId, int AccountId)> newKeys)
		{
			var addedKeys = newKeys.Except(existingKeys);
			foreach (var (TransactionId, AccountId) in addedKeys)
			{
				var activitiesToAdd = activities.Where(x => 
					x.TransactionId == TransactionId && 
					x.Account.Id == AccountId);
				await databaseContext.Activities.AddRangeAsync(activitiesToAdd);
			}
		}

		private static async Task UpdateExistingActivities(
			DatabaseContext databaseContext,
			List<Activity> existingActivities,
			IEnumerable<Activity> activities,
			List<(string TransactionId, int AccountId)> existingKeys,
			List<(string TransactionId, int AccountId)> newKeys)
		{
			var updatedKeys = existingKeys.Intersect(newKeys);
			foreach (var (TransactionId, AccountId) in updatedKeys)
			{
				var existingActivity = existingActivities
					.Where(x => x.TransactionId == TransactionId && 
								x.Account.Id == AccountId)
					.OrderBy(x => x.SortingPriority)
					.ThenBy(x => x.Description);

				var newActivity = activities
					.Where(x => x.TransactionId == TransactionId && 
								x.Account.Id == AccountId)
					.OrderBy(x => x.SortingPriority)
					.ThenBy(x => x.Description);

				if (!AreActivitiesEqual(existingActivity, newActivity))
				{
					databaseContext.Activities.RemoveRange(existingActivity);
					await databaseContext.Activities.AddRangeAsync(newActivity);
				}
			}
		}

		private static bool AreActivitiesEqual(IEnumerable<Activity> existing, IEnumerable<Activity> newActivities)
		{
			var compareLogic = new CompareLogic()
			{
				Config = new ComparisonConfig
				{
					MaxDifferences = int.MaxValue,
					IgnoreObjectTypes = true,
					MembersToIgnore = [
						nameof(Activity.Id), 
						nameof(ActivityWithQuantityAndUnitPrice.AdjustedQuantity),
						nameof(ActivityWithQuantityAndUnitPrice.AdjustedUnitPrice),
						nameof(ActivityWithQuantityAndUnitPrice.AdjustedUnitPriceSource),
						nameof(BuyActivityFee.ActivityId)]
				}
			};
			
			return compareLogic.Compare(existing, newActivities).AreEqual;
		}
	}
}
