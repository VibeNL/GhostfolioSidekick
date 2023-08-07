using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{

	public class ScalableCapitalParser : IFileImporter
	{
		private GhostfolioAPI api;

		public ScalableCapitalParser(GhostfolioAPI api)
		{
			this.api = api;
		}

		protected IEnumerable<HeaderMapping> ExpectedHeaders => new[]
		{
					new HeaderMapping{ DestinationHeader = DestinationHeader.Date, SourceName = "XXX-BUDAT" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.UnitPrice, SourceName ="XXX-WPKURS" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.Currency, SourceName ="XXX-WHGAB" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.Quantity, SourceName = "XXX-NW" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.Isin, SourceName = "XXX-WPNR" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.OrderType, SourceName = "XXX-WPGART" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.Reference, SourceName = "XXX-EXTORDID" },
					new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName = "XXX-BELEGU" },
				};

		public Task<bool> CanConvertOrders(string file)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<Order>> ConvertToOrders(string accountName, string filename)
		{
			throw new NotImplementedException();
		}
	}
}
