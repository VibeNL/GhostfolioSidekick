using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParserNL : DeGiroParserBase<DeGiroRecordNL>
	{
		public DeGiroParserNL(IGhostfolioAPI api) : base(api)
		{
		}
	}
}