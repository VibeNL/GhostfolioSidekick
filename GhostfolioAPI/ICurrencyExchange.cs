using GhostfolioSidekick.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface ICurrencyExchange
	{
		Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date);
	}
}
