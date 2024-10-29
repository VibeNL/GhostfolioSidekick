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
	public class Holding
	{
		public int Id { get; set; }

		public virtual List<SymbolProfile> SymbolProfiles { get; set; } = [];

		public virtual List<Activity> Activities { get; set; } = [];

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public void MergeIdentifiers(IList<PartialSymbolIdentifier> ids)
		{
			foreach (var item in ids)
			{
				if (!PartialSymbolIdentifiers.Any(x => x == item))
				{
					PartialSymbolIdentifiers.Add(item);
				}
			}
		}

		[ExcludeFromCodeCoverage]
		override public string ToString()
		{
			return $"{SymbolProfiles.FirstOrDefault()?.Symbol} - {Activities.Count} activities";
		}
	}
}
