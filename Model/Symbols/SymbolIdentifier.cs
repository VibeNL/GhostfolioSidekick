using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Symbols
{
	public class SymbolIdentifier
	{
		public string Identifier { get; set; } = string.Empty!;

		public Currency? Currency { get; set; }

		public IdentifierType IdentifierType { get; set; }
	}
}