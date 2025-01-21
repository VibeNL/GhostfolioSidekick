using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Model.Activities.Types.MoneyLists
{
	public record BuySellActivityFee
	{
		public BuySellActivityFee() : base()
		{
			Money = default!;
		}

		public BuySellActivityFee(Money money)
		{
			Money = money;
		}

		public int? Id { get; set; }

		public Money Money { get; }
	}
}
