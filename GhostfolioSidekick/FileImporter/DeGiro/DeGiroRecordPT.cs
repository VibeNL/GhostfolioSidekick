using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordPT : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Data")]
		public override DateOnly Date { get; set; }

		[Name("Hora")]
		public override TimeOnly Time { get; set; }

		[Name("Data Valor")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Produto")]
		public override string Product { get; set; }

		[Name("ISIN")]
		public override string ISIN { get; set; }

		[Name("Descrição")]
		public override string Description { get; set; }

		[Name("T.")]
		public override string FX { get; set; }

		[Name("Mudança")]
		public override string Mutation { get; set; }
		
		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Saldo")]
		public override string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("ID da Ordem")]
		public override string TransactionId { get; set; }
	}
}
