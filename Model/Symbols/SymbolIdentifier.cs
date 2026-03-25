using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Symbols
{
	public record SymbolIdentifier
	{
		public string Identifier { get; init; } = string.Empty!;

		public Currency? Currency { get; init; }

		public IdentifierType IdentifierType { get; init; }
	}
}