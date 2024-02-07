using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;
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
		private readonly IEnumerable<IFileImporter> importers;
		private readonly IEnumerable<IHoldingStrategy> strategies;

		public TaskPriority Priority => TaskPriority.FileImporter;

		public FileImporterTask(
			ILogger<FileImporterTask> logger,
			IApplicationSettings settings,
			IActivitiesService activitiesManager,
			IAccountService accountManager,
			IMarketDataService marketDataManager,
			IExchangeRateService exchangeRateService,
			IEnumerable<IFileImporter> importers,
			IEnumerable<IHoldingStrategy> strategies)
		{
			ArgumentNullException.ThrowIfNull(settings);

			fileLocation = settings.FileImporterPath;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.activitiesManager = activitiesManager ?? throw new ArgumentNullException(nameof(activitiesManager));
			this.accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
			this.marketDataManager = marketDataManager ?? throw new ArgumentNullException(nameof(marketDataManager));
			this.exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
			this.importers = importers ?? throw new ArgumentNullException(nameof(importers));
			this.strategies = strategies;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(FileImporterTask)} Starting to do work");

			var directories = Directory.GetDirectories(fileLocation);

			var holdingsCollection = new HoldingsCollection(accountManager, marketDataManager);
			var accountNames = new List<string>();
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

					accountNames.Add(accountName);
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
				}
			}

			await holdingsCollection.GenerateActivities();

			ApplyHoldingActions(holdingsCollection, strategies);

			var existingHoldings = await activitiesManager.GetAllActivities();
			var mergeOrders = (await new MergeActivities(exchangeRateService)
				.Merge(existingHoldings, holdingsCollection.Holdings))
				.Where(x => x.Order1.ActivityType != Model.Activities.ActivityType.KnownBalance)
				.Where(x => x.Operation != Operation.Duplicate)
				.OrderBy(x => x.Order1.Date);

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

			var accounts = await holdingsCollection.UpdateAccountBalances(exchangeRateService);
			foreach (var account in accounts)
			{
				try
				{
					await accountManager.UpdateBalance(account, account.Balance);
					logger.LogInformation($"Update account {account.Name} with balance {account.Balance}");
				}
				catch (Exception ex)
				{
					logger.LogError($"Account balance for account {account.Name} failed to update {ex}, skipping");
				}
			}

			logger.LogInformation($"{nameof(FileImporterTask)} Done");
		}

		private void ApplyHoldingActions(HoldingsCollection holdingsCollection, IEnumerable<IHoldingStrategy> strategies)
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
