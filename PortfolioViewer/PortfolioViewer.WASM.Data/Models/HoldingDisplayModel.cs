using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{

	public class HoldingDisplayModel
	{
		public string Symbol { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public Money CurrentValue { get; set; }
		public decimal Quantity { get; set; }
		public Money AveragePrice { get; set; }
		public Money CurrentPrice { get; set; }
		public Money GainLoss { get; set; }
		public decimal GainLossPercentage { get; set; }
		public decimal Weight { get; set; }
		public string Sector { get; set; } = string.Empty;
		public string AssetClass { get; set; } = string.Empty;
		public string Currency { get; set; } = "USD";

		public override string ToString()
		{
			return $"{Name} ({Symbol}) - Current Value: {CurrentValue}, Quantity: {Quantity}, Average Price: {AveragePrice}, Current Price: {CurrentPrice}, Gain/Loss: {GainLoss}, Gain/Loss Percentage: {GainLossPercentage}%, Weight: {Weight}%, Sector: {Sector}, Asset Class: {AssetClass}, Currency: {Currency}";
		}
	}
}