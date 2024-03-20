using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Strategies;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GhostfolioSidekick.FileImporter
{
	public class FileImporterTask : IScheduledWork
	{
		private readonly string fileLocation;
		private readonly ILogger<FileImporterTask> logger;
		private readonly IActivitiesService activitiesManager;
		private readonly IAccountService accountManager;
		private readonly IMarketDataService marketDataManager;
		private readonly IExchangeRateService exchangeRateService;
		private readonly IEnumerable<ITransactionFileImporter> importers;
		private readonly IEnumerable<IHoldingStrategy> strategies;
		private readonly IMemoryCache memoryCache;

		public TaskPriority Priority => TaskPriority.FileImporter;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IApplicationSettings settings,
			IActivitiesService activitiesManager,
			IAccountService accountManager,
			IMarketDataService marketDataManager,
			IExchangeRateService exchangeRateService,
			IEnumerable<ITransactionFileImporter> importers,
			IEnumerable<IHoldingStrategy> strategies,
			IMemoryCache memoryCache)
		{
			fileLocation = settings.FileImporterPath;
			this.logger = logger;
			this.activitiesManager = activitiesManager;
			this.accountManager = accountManager;
			this.marketDataManager = marketDataManager;
			this.exchangeRateService = exchangeRateService;
			this.importers = importers;
			this.strategies = strategies;
			this.memoryCache = memoryCache;
		}

		public async Task DoWork()
		{
			var directories = Directory.GetDirectories(fileLocation);

			string fileHashes = CalculateHash(directories);
			var knownHash = memoryCache.TryGetValue(nameof(FileImporterTask), out string? hash) ? hash : string.Empty;
			if (fileHashes == knownHash)
			{
				logger.LogDebug($"{nameof(FileImporterTask)} Skip to do work, no file changes detected");
				return;
			}

			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var holdingsCollection = new HoldingsCollection(logger, accountManager, marketDataManager);
			var accountNames = new List<string>();
			foreach (var directory in directories.Select(x => new DirectoryInfo(x)).OrderBy(x => x.Name))
			{
				var accountName = directory.Name;

				logger.LogInformation($"Parsing files for account: {accountName}");

				try
				{
					var files = directory.GetFiles("*.*", SearchOption.AllDirectories).Select(x => x.FullName)
						.Where(x => x.EndsWith("csv", StringComparison.InvariantCultureIgnoreCase) ||
									x.EndsWith("pdf", StringComparison.InvariantCultureIgnoreCase));

					foreach (var file in files)
					{
						var importer = importers.SingleOrDefault(x => x.CanParseActivities(file).Result) ?? throw new NoImporterAvailableException($"File {file} has no importer");
						await importer.ParseActivities(file, holdingsCollection, accountName);
					}

					accountNames.Add(accountName);
				}
				catch (NoImporterAvailableException)
				{
					var sb = new StringBuilder();
					sb.AppendLine($"No importer available for {accountName}");

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
				}
			}

			logger.LogInformation($"Generating activities");
			await holdingsCollection.GenerateActivities(exchangeRateService);

			logger.LogInformation($"Applying strategies");
			ApplyHoldingActions(holdingsCollection, strategies);

			// Only update accounts when we have at least one transaction
			var managedAccount = holdingsCollection
				.Holdings
				.SelectMany(x => x.Activities).Select(x => x.Account.Id)
				.Distinct()
				.ToList();

			logger.LogInformation($"Detecting changes");
			var existingHoldings = await activitiesManager.GetAllActivities();
			var mergeOrders = (await new MergeActivities(exchangeRateService)
				.Merge(existingHoldings, holdingsCollection.Holdings))
				.Where(x => x.Order1 is not KnownBalanceActivity)
				.Where(x => x.Operation != Operation.Duplicate)
				.Where(x => managedAccount.Contains(x.Order1.Account.Id))
				.OrderBy(x => x.Order1.Date);

			logger.LogInformation($"Applying changes");
			foreach (var item in mergeOrders)
			{
				try
				{
					switch (item.Operation)
					{
						case Operation.New:
							await activitiesManager.InsertActivity(item.SymbolProfile, item.Order1);
							break;
						case Operation.Updated:
							await activitiesManager.DeleteActivity(item.SymbolProfile, item.Order1);
							await activitiesManager.InsertActivity(item.SymbolProfile, item.Order2!);
							break;
						case Operation.Removed:
							await activitiesManager.DeleteActivity(item.SymbolProfile, item.Order1);
							break;
						default:
							throw new NotSupportedException();
					}
				}
				catch (Exception ex)
				{
					logger.LogError($"Transaction failed to write {ex}, skipping");
				}
			}

			logger.LogInformation($"Setting balances");
			foreach (var balance in holdingsCollection.Balances)
			{
				var existingAccount = (await accountManager.GetAccountByName(balance.Key))!;

				if (Math.Abs(existingAccount.Balance.Money.Amount - balance.Value.Money.Amount) < Constants.Epsilon)
				{
					logger.LogDebug($"Account {balance.Key} balance unchanged on: {balance.Value.Money.Amount}");
					continue;
				}

				try
				{
					await accountManager.UpdateBalance(existingAccount, balance.Value);
					logger.LogInformation($"Set account {balance.Key} balance to: {balance.Value}");
				}
				catch (Exception ex)
				{
					logger.LogError($"Account balance for account {balance.Key} failed to update {ex}, skipping");
				}
			}

			memoryCache.Set(nameof(FileImporterTask), fileHashes, TimeSpan.FromHours(1));

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
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

		private static void ApplyHoldingActions(HoldingsCollection holdingsCollection, IEnumerable<IHoldingStrategy> strategies)
		{
			foreach (var strategy in strategies.OrderBy(x => x.Priority))
			{
				foreach (var holding in holdingsCollection.Holdings)
				{
					strategy.Execute(holding);
				}
			}
		}
	}
}
