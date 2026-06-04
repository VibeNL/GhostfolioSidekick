using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Portfolio
{
	/// <summary>
	/// Exposes portfolio data functions as AI tools via the <see cref="IAgentToolProvider"/> extension point.
	/// </summary>
	public class PortfolioAgentToolProvider : IAgentToolProvider
	{
		public string ProviderName => "PortfolioData";

		public string ProviderDescription =>
			"Provides direct access to the user's portfolio: current holdings, account summary, upcoming dividends, and historical performance.";

		private readonly IReadOnlyList<AITool> _tools;

		public PortfolioAgentToolProvider(IServiceScopeFactory scopeFactory)
		{
			var functions = new PortfolioAgentFunction(scopeFactory);

			_tools =
			[
				AIFunctionFactory.Create(functions.GetHoldings, "get_holdings"),
				AIFunctionFactory.Create(functions.GetPortfolioSummary, "get_portfolio_summary"),
				AIFunctionFactory.Create(functions.GetUpcomingDividends, "get_upcoming_dividends"),
				AIFunctionFactory.Create(functions.GetPortfolioPerformance, "get_portfolio_performance"),
			];
		}

		public IReadOnlyList<AITool> GetTools() => _tools;
	}
}
