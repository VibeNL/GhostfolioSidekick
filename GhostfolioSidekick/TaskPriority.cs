namespace GhostfolioSidekick.ProcessingService
{
	public enum TaskPriority
	{
		DisplayInformation,

		GenerateDatabase,

		AccountMaintainer,

		MaintainManualSymbol,

		SyncManualActivitiesWithGhostfolio,

		FileImporter,
		
		CurrencyGatherer,

		BalanceMaintainer,

		DetermineHoldings,

		SymbolMatcher,
		
		MarketDataStockSplit,

		MarketDataGatherer,
		
		CalculatePrice,

		CleanupDatabase,

		SyncAccountsWithGhostfolio,

		SyncActivitiesWithGhostfolio,

		CleanupGhostfolio,

		//SetManualPrices,

		//DeleteUnusedSymbols,

		//SetTrackingInsightOnSymbols,

		//SetBenchmarks,

		//GatherAllData,
	}
}
