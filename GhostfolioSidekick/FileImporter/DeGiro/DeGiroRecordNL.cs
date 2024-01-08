using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordNL : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Datum")]
		public override DateOnly Date { get; set; }

		[Name("Tijd")]
		public override TimeOnly Time { get; set; }

		[Name("Valutadatum")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Product")]
		public override string Product { get; set; }

		[Name("ISIN")]
		public override string ISIN { get; set; }

		[Name("Omschrijving")]
		public override string Description { get; set; }

		[Name("FX")]
		public override string FX { get; set; }

		[Name("Mutatie")]
		public override string Mutation { get; set; }
		
		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Saldo")]
		public override string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("Order Id")]
		public override string TransactionId { get; set; }
	}
}
