using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public interface IServerCurrencyConversion
	{
		Task<string> ConvertTableNameInCaseOfPrimaryCurrency(string tableName);

		Task<List<Dictionary<string, object>>> ConvertTableToPrimaryCurrencyTable(List<Dictionary<string, object>> data, string tableName, Currency targetCurrency);
	}
}