namespace GhostfolioSidekick
{
	public enum TaskPriority
	{
		DisplayInformation,

		PrepareDatabaseTask, // TODO: Move after manual prices

		AccountCreation,

		CreateManualSymbols,

		SetManualPrices,

		FileImporter,

		DeleteUnusedSymbols,

		SetTrackingInsightOnSymbols,

		SetBenchmarks,

		GatherAllData,
	}
}
