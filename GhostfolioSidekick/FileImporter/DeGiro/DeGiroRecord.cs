﻿using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecord
	{
		[Format("dd-MM-yyyy")]
		public DateOnly Datum { get; set; }

		public TimeOnly Tijd { get; set; }

		[Format("dd-MM-yyyy")]
		public DateOnly Valutadatum { get; set; }

		public string Product { get; set; }

		public string ISIN { get; set; }

		public string Omschrijving { get; set; }

		public string FX { get; set; }

		public string Mutatie { get; set; }

		[Index(8)]
		public decimal? Total { get; set; }

		public string Saldo { get; set; }

		[Index(10)]
		public decimal SaldoValue { get; set; }

		[Name("Order Id")]
		public string OrderId { get; set; }


	}
}
