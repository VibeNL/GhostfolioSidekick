using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class PortfolioValueHistoryPoint
    {
        public DateOnly Date { get; set; }
        
		public Money[] Value { get; set; }

		public Money[] Invested { get; set; }
	}
}