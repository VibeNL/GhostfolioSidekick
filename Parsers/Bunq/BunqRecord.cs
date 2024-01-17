using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.Bunq
{
	public class BunqRecord
	{
		public DateTime Date { get; set; }

		[Name("Interest Date")]
		public required string InterestDate { get; set; }

		[CultureInfo("nl-NL")]
		public decimal Amount { get; set; }

		public required string Name { get; set; }

		public required string Description { get; set; }

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
