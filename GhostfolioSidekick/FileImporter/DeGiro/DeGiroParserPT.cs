using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParserPT : DeGiroParserBase<DeGiroRecordPT>
	{
		public DeGiroParserPT(IGhostfolioAPI api) : base(api)
		{
		}
	}
}