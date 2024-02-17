using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.FileImporter
{
	[Serializable]
	internal class SymbolNotFoundException : Exception
	{
		public SymbolNotFoundException(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			SymbolIdentifiers = symbolIdentifiers;
		}

		public PartialSymbolIdentifier[] SymbolIdentifiers { get; }
	}
}