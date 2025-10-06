using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecord
	{
		[Format("dd-MM-yyyy")]
		[Name("Date",  // EN
				"Datum", // NL
				"Data")] // PT
		public DateOnly Date { get; set; }

		[Name("Time",  // EN
				"Tijd",  // NL
				"Hora")] // PT
		public TimeOnly Time { get; set; }

		[Format("dd-MM-yyyy")]
		[Name("Value date",   // EN	
				"Valutadatum",  // NL
				"Data Valor")]  // PT
		public DateOnly CurrencyDate { get; set; }

		[Name("Product",    // EN & NL
				"Produto")] // PT
		public string? Product { get; set; }

		[Name("ISIN")]
		public string? ISIN { get; set; }

		[Name("Description",    // EN
				"Omschrijving", // NL
				"Descrição")]   // PT
		public required string Description { get; set; }

		[Name("FX",   // EN & NL
				"T.")]  // PT
		public string? FX { get; set; }

		[Name("Change",   // EN
				"Mutatie",  // NL
				"Mudança")] // PT
		public required string Mutation { get; set; }

		[Index(8)]
		public required string Total { get; set; }

		[Name("Balance",    // EN
				"Saldo")]   // NL & PT
		public required string BalanceCurrency { get; set; }

		[Index(10)]
		public required string Balance { get; set; }

		[Name("Order Id",     // EN & NL
				"ID da Ordem")] // PT
		public string? TransactionId { get; set; }
	}
}
