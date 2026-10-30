using CsvHelper;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	[CsvRecord]
	public partial class DeGiroRecord
	{
		[CsvFormat("dd-MM-yyyy")]
		[CsvColumn("Date",  // EN
				"Datum", // NL
				"Data")] // PT
		public DateOnly Date { get; set; }

		[CsvColumn("Time",  // EN
				"Tijd",  // NL
				"Hora")] // PT
		public TimeOnly Time { get; set; }

		[CsvFormat("dd-MM-yyyy")]
		[CsvColumn("Value date",   // EN	
				"Valutadatum",  // NL
				"Data Valor")]  // PT
		public DateOnly CurrencyDate { get; set; }

		[CsvColumn("Product",    // EN & NL
				"Produto")] // PT
		public string? Product { get; set; }

		[CsvColumn("ISIN")]
		public string? ISIN { get; set; }

		[CsvColumn("Description",    // EN
				"Omschrijving", // NL
				"Descrição")]   // PT
		public required string Description { get; set; }

		[CsvColumn("FX",   // EN & NL
				"T.")]  // PT
		public string? FX { get; set; }

		[CsvColumn("Change",   // EN
				"Mutatie",  // NL
				"Mudança")] // PT
		public required string Mutation { get; set; }

		[CsvColumn(Index = 8)]
		public required string Total { get; set; }

		[CsvColumn("Balance",    // EN
				"Saldo")]   // NL & PT
		public required string BalanceCurrency { get; set; }

		[CsvColumn(Index = 10)]
		public required string Balance { get; set; }

		[CsvColumn("Order Id",     // EN & NL
				"ID da Ordem")] // PT
		public string? TransactionId { get; set; }
	}
}
