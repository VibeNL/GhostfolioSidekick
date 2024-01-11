using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	[Delimiter(",")]
	public class CoinbaseRecord
	{
		[Format("yyyy-MM-ddTHH:mm:ssZ")]
		public DateTime Timestamp { get; set; }

		[Name("Transaction Type")]
		public required string Type { get; set; }

		public required string Asset { get; set; }

		[Name("Quantity Transacted")]
		[NumberStyles(NumberStyles.Number | NumberStyles.AllowExponent)]
		public decimal Quantity { get; set; }

		[Name("Spot Price Currency")]
		public required string Currency { get; set; }

		[Name("Spot Price at Transaction")]
		public decimal? Price { get; set; }

		[Name("Fees and/or Spread")]
		public decimal? Fee { get; set; }

		public required string Notes { get; set; }

		[LineNumber]
		public int RowNumber { get; set; }

		private class LineNumber : Attribute, IMemberMapper, IParameterMapper
		{
			public void ApplyTo(MemberMap memberMap)
			{
				memberMap.Data.ReadingConvertExpression = (ConvertFromStringArgs args) => args.Row.Parser.Row;
			}

			public void ApplyTo(ParameterMap parameterMap)
			{
			}
		}
	}
}