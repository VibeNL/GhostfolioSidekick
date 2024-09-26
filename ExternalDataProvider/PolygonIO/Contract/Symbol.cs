using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract
{
	public class Symbol
	{
		// Trading volume
		public bool Active { get; set; }

		public string CIK { get; set; }

		public string CompositeFIGI { get; set; }

		public string CurrencyName { get; set; }

		public string DelistedUTC { get; set; }

		public string LastUpdatedUTC { get; set; }

		public string Locale { get; set; }

		public string Market { get; set; }

		public string Name { get; set; }

		public string PrimaryExchange { get; set; }

		public string ShareClassFIGI { get; set; }

		public string Ticker { get; set; }

		public string Type { get; set; }
	}
}
