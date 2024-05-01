using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class BalanceList
	{
		public required Balance[] Balances { get; set; }
	}
}
