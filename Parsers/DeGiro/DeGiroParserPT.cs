using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParserPT : DeGiroParserBase<DeGiroRecordPT>
	{
		public DeGiroParserPT(IGhostfolioAPI api) : base(api)
		{
		}
	}
}