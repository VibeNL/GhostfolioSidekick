using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider.DividendMax
{
	/// <summary>
	/// Uses DividendMax.com to gather upcoming dividends.
	/// 
	/// Follow these steps:
	/// 1) https://www.dividendmax.com/suggest.json?q={symbol}
	/// 2) follow the URL in path (sub url) to get the website
	/// 3) parse the table (class="mdc-data-table__table") with the following columns:
	///		Status, Type, Decl. date, Ex-div date, Pay date, Decl. Currency, Forecast amount, Decl. amount, Accuracy
	/// 4) generate UpcomingDividend objects from the rows where Ex-div date is in the future. and the decl. amount is not empty / a '-'.
	/// 
	/// </summary>
	internal class DividendMax : IUpcomingDividendRepository
	{




		public Task<IList<UpcomingDividend>> Gather(SymbolProfile symbol)
		{
			
		}
	}
}
