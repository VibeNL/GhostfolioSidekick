using GhostfolioSidekick.Model.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	public class Holding
	{
		public Holding(SymbolProfile? symbolProfile)
		{
			SymbolProfile = symbolProfile;
		}

		public SymbolProfile? SymbolProfile { get; }

		public List<IActivity> Activities { get; set; } = [];

		[ExcludeFromCodeCoverage]
		override public string ToString()
		{
			return $"{SymbolProfile?.Symbol} - {Activities.Count} activities";
		}
	}
}
