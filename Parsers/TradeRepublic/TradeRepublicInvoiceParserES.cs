using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserES : TradeRepublicInvoiceParserBase
	{
		// ES
		protected override string Keyword_Position => "POSICIÓN";
		protected override string Keyword_Quantity => "CANTIDAD";
		protected override string Keyword_Quantity_PiecesText => "tít.";
		protected override string[] Keyword_Price => ["COTIZACIÓN", "PRECIO"];
		protected override string Keyword_Amount => "IMPORTE";
		protected override string[] Keyword_Nominal => ["NOMINAL", "NOMINALES"];
		protected override string Keyword_Income => "RENDIMIENTO";
		protected override string Keyword_Coupon => "CUPÓN";
		protected override string Keyword_Total => "TOTAL";
		protected override string Keyword_AverageRate => "COTIZACIÓN PROMEDIO";
		protected override string[] Keyword_Booking => ["RESERVA",   // For "buy" operations
														"REGISTRO"]; // For dividends/repayments/interests
		protected override string Keyword_Security => "INSTRUMENTO";
		protected override string Keyword_Number => "NÚM.";
		protected override string SECURITIES_SETTLEMENT => "LIQUIDACIÓN DE VALORES";
		protected override string DIVIDEND => "DIVIDENDO";
		protected override string INTEREST_PAYMENT => "PAGO DE INTERESES";
		protected override string REPAYMENT => "VENCIMIENTO FINAL";
		protected override string ACCRUED_INTEREST => "Interés acumulado";
		protected override string EXTERNAL_COST_SURCHARGE => "del servicio de ejecución de terceros";
		protected override string WITHHOLDING_TAX => "Retención Fiscal para Emisores de Estados Unidos";
		protected override string DATE => "FECHA";
		protected override CultureInfo Culture => new CultureInfo("es");

		public TradeRepublicInvoiceParserES(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}

		protected override bool CheckEndOfRecord(List<MultiWordToken> headers, string currentWord)
		{
			var headerMappings = new List<string[]>
			{
				{ [ "POSICIÓN", "CANTIDAD", "RENDIMIENTO", "CANTIDAD" ] }, // dividend
				{ [ "NÚM.", "REGISTRO", "INSTRUMENTO", "CANTIDAD" ] }, // repay bond
				{ [ "POSICIÓN", "NOMINALES", "CUPÓN", "CANTIDAD" ] } // interest bond
			};

			foreach (var mapping in headerMappings)
			{
				if (headers.Select(x => x.KeyWord).SequenceEqual(mapping) && mapping.Last() == currentWord)
				{
					return true;
				}
			}

			return false;
		}
	}
}
