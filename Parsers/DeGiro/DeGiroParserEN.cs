using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParserEN : DeGiroParserBase<DeGiroRecordEN>
	{
		public DeGiroParserEN(ICurrencyMapper currencyMapper) : base(currencyMapper)
		{
		}
	}
}