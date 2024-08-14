using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers
{
	public interface ICurrencyMapper
	{
		Currency Map(string currency);
	}
}
