using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model
{
	public class Holding(SymbolProfile? symbolProfile)
	{
		public SymbolProfile? SymbolProfile { get; } = symbolProfile;

		public List<Activity> Activities { get; set; } = [];

		[ExcludeFromCodeCoverage]
		override public string ToString()
		{
			return $"{SymbolProfile?.Symbol} - {Activities.Count} activities";
		}
	}
}
