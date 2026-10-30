using CsvHelper;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	[CsvRecord]
	public partial class GenericRecord
	{
		public PartialActivityType ActivityType { get; set; }

		[CsvColumn(Optional = true)]
		[CsvConverter(typeof(AssetClassListConverter))]
		public List<AssetClass> AssetClass { get; set; } = [];

		[CsvColumn(Optional = true)]
		[CsvConverter(typeof(AssetSubClassListConverter))]
		public List<AssetSubClass> AssetSubClass { get; set; } = [];

		public string? Symbol { get; set; }

		[CsvColumn(Optional = true)]
		public string? ISIN { get; set; }

		[CsvColumn(Optional = true)]
		public string? Name { get; set; }

		[CsvConverter(typeof(UtcDateTimeConverter))]
		public DateTime Date { get; set; }

		public required string Currency { get; set; }

		public decimal Quantity { get; set; }

		public decimal UnitPrice { get; set; }

		[CsvColumn(Optional = true)]
		public decimal? Fee { get; set; }

		[CsvColumn(Optional = true)]
		public decimal? Tax { get; set; }

		[CsvColumn(Optional = true)]
		public string? Id { get; set; }
	}

	public sealed class AssetClassListConverter : ICsvConverter<List<AssetClass>>
	{
		public List<AssetClass> Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			if (string.IsNullOrWhiteSpace(text))
			{
				return new List<AssetClass>();
			}
			return text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => Enum.TryParse<AssetClass>(x.Trim(), true, out var val) ? val : AssetClass.Undefined)
					.Where(x => x != AssetClass.Undefined)
					.ToList();
		}

		public void Write(CsvWriter writer, List<AssetClass> value)
		{
			writer.WriteField(string.Join(";", value));
		}
	}

	public sealed class AssetSubClassListConverter : ICsvConverter<List<AssetSubClass>>
	{
		public List<AssetSubClass> Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			if (string.IsNullOrWhiteSpace(text))
			{
				return new List<AssetSubClass>();
			}

			return text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => Enum.TryParse<AssetSubClass>(x.Trim(), true, out var val) ? val : AssetSubClass.Undefined)
					.Where(x => x != AssetSubClass.Undefined)
					.ToList();
		}

		public void Write(CsvWriter writer, List<AssetSubClass> value)
		{
			writer.WriteField(string.Join(";", value));
		}
	}
}
