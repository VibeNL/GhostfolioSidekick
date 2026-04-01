using CsvHelper.Configuration.Attributes;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericRecord
	{
		public PartialActivityType ActivityType { get; set; }

		[Optional]
		[TypeConverter(typeof(AssetClassListConverter))]
		public List<AssetClass> AssetClass { get; set; } = new();

		[Optional]
		[TypeConverter(typeof(AssetSubClassListConverter))]
		public List<AssetSubClass> AssetSubClass { get; set; } = new();

		public string? Symbol { get; set; }

		[Optional]
		public string? ISIN { get; set; }

		[Optional]
		public string? Name { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		public DateTime Date { get; set; }

		public required string Currency { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitPrice { get; set; }

		[Optional]
		public decimal? Fee { get; set; }

		[Optional]
		public decimal? Tax { get; set; }

		[Optional]
		public string? Id { get; set; }
	}

	public class AssetClassListConverter : DefaultTypeConverter
	{
		public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return new List<AssetClass>();
			}
			return text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => Enum.TryParse<AssetClass>(x.Trim(), true, out var val) ? val : AssetClass.Undefined)
					.Where(x => x != AssetClass.Undefined)
					.ToList();
		}
	}

	public class AssetSubClassListConverter : DefaultTypeConverter
	{
		public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return new List<AssetSubClass>();
			}

			return text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => Enum.TryParse<AssetSubClass>(x.Trim(), true, out var val) ? val : AssetSubClass.Undefined)
					.Where(x => x != AssetSubClass.Undefined)
					.ToList();
		}
	}
}
