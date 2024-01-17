using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParserNL : DeGiroParserBase<DeGiroRecordNL>
	{
		public DeGiroParserNL(IGhostfolioAPI api) : base(api)
		{
		}
	}
}