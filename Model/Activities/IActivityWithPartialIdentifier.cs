using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivityWithPartialIdentifier
	{
		ICollection<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }
	}
}
