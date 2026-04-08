using System;
using System.Collections.Generic;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class TaxAccountDisplayModel
	{
       public string? Name { get; set; }
       public string? AccountType { get; set; }
       public List<TaxHoldingDisplayModel> Holdings { get; set; } = new();
       public List<TaxHoldingDisplayModel> StartHoldings { get; set; } = new();
       public List<TaxHoldingDisplayModel> EndHoldings { get; set; } = new();
       public decimal StartValue { get; set; }
       public decimal EndValue { get; set; }
       public decimal StartCashBalance { get; set; }
       public decimal EndCashBalance { get; set; }
       public List<TaxTransactionDisplayModel> Transactions { get; set; } = new();
       public List<TaxDividendDisplayModel> Dividends { get; set; } = new();
       public List<TaxGainLossDisplayModel> RealizedGainsLosses { get; set; } = new();

       // Performance optimization: precomputed symbol rows and totals
       public List<SymbolRow> SymbolRows { get; set; } = new();
       public decimal TotalStartValue { get; set; }
       public decimal TotalEndValue { get; set; }

   public class SymbolRow
   {
       public string Symbol { get; set; } = string.Empty;
       public decimal StartQuantity { get; set; }
       public decimal StartValue { get; set; }
       public decimal EndQuantity { get; set; }
       public decimal EndValue { get; set; }
   }
	}

	public class TaxHoldingDisplayModel
	{
		public string? Symbol { get; set; }
		public decimal Quantity { get; set; }
		public decimal Value { get; set; }
		public DateTime AcquisitionDate { get; set; }
	}

	public class TaxTransactionDisplayModel
	{
		public DateTime Date { get; set; }
		public string? Type { get; set; }
		public string? Symbol { get; set; }
		public decimal Amount { get; set; }
		public decimal Fees { get; set; }
		public string? Notes { get; set; }
	}

	public class TaxDividendDisplayModel
	{
		public DateTime Date { get; set; }
		public string? Symbol { get; set; }
		public decimal Amount { get; set; }
		public decimal TaxWithheld { get; set; }
	}

	public class TaxGainLossDisplayModel
	{
		public string? Symbol { get; set; }
		public decimal Amount { get; set; }
		public DateTime Date { get; set; }
	}
}
