using AwesomeAssertions;
using CsvHelper;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public class CsvConverterTests
	{
		private static CsvReader ReadSingleRow(string value)
		{
			var reader = CsvReader.FromText(value, new CsvReaderOptions { HasHeader = false });
			reader.Read().Should().BeTrue();
			return reader;
		}

		[Theory]
		[InlineData("1.5E-05", "0.000015")]
		[InlineData("2e3", "2000")]
		[InlineData("1234.5678", "1234.5678")]
		[InlineData("-3.2E+2", "-320")]
		public void ExponentDecimalConverter_Read_ParsesValue(string input, string expected)
		{
			using var reader = ReadSingleRow(input);

			var result = new ExponentDecimalConverter().Read(reader, 0);

			result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
		}

		[Fact]
		public void ExponentDecimalConverter_Read_Invalid_Throws()
		{
			using var reader = ReadSingleRow("abc");

			var act = () => new ExponentDecimalConverter().Read(reader, 0);

			act.Should().Throw<FormatException>();
		}

		[Theory]
		[InlineData("1.5E-05", "0.000015")]
		[InlineData("42", "42")]
		public void ExponentNullableDecimalConverter_Read_ParsesValue(string input, string expected)
		{
			using var reader = ReadSingleRow(input);

			var result = new ExponentNullableDecimalConverter().Read(reader, 0);

			result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
		}

		[Fact]
		public void ExponentNullableDecimalConverter_Read_Empty_ReturnsNull()
		{
			using var reader = ReadSingleRow("x,");

			var result = new ExponentNullableDecimalConverter().Read(reader, 1);

			result.Should().BeNull();
		}

		[Theory]
		[InlineData("1.5E-05", "0.000015")]
		[InlineData("€1.5E-05", "0.000015")]
		[InlineData("EUR 2e3", "2000")]
		[InlineData("€1,234.50", "1,234.50")]
		[InlineData("-€5.25", "-5.25")]
		public void DecimalCurrencyStrippingConverter_Read_ParsesValue(string input, string expected)
		{
			using var reader = ReadSingleRow($"\"{input}\"");

			var result = new GhostfolioSidekick.Parsers.Coinbase.DecimalCurrencyStrippingConverter().Read(reader, 0);

			result.Should().Be(decimal.Parse(expected, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture));
		}

		[Theory]
		[InlineData("2023-08-07T19:56:02Z", 2023, 8, 7, 19, 56, 2)]
		[InlineData("2023-08-07 19:56:02 UTC", 2023, 8, 7, 19, 56, 2)]
		[InlineData("2023-08-07 19:56 UTC", 2023, 8, 7, 19, 56, 0)]
		[InlineData("2023-08-07 19:56:02", 2023, 8, 7, 19, 56, 2)]
		[InlineData("2023-08-07", 2023, 8, 7, 0, 0, 0)]
		[InlineData("07/08/2023", 2023, 7, 8, 0, 0, 0)]
		[InlineData("07-08-2023", 2023, 7, 8, 0, 0, 0)]
		public void UtcDateTimeConverter_Read_ParsesAsUtc(string input, int year, int month, int day, int hour, int minute, int second)
		{
			using var reader = ReadSingleRow(input);

			var result = new UtcDateTimeConverter().Read(reader, 0);

			result.ToUniversalTime().Should().Be(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc));
		}

		[Theory]
		[InlineData("07/08/2023")]
		[InlineData("07-08-2023")]
		public void DayFirstUtcDateTimeConverter_Read_ParsesDayFirst(string input)
		{
			using var reader = ReadSingleRow(input);

			var result = new DayFirstUtcDateTimeConverter().Read(reader, 0);

			result.ToUniversalTime().Should().Be(new DateTime(2023, 8, 7, 0, 0, 0, DateTimeKind.Utc));
		}

		[Fact]
		public void UtcNullableDateTimeConverter_Read_Empty_ReturnsNull()
		{
			using var reader = ReadSingleRow("x,");

			var result = new UtcNullableDateTimeConverter().Read(reader, 1);

			result.Should().BeNull();
		}

		[Fact]
		public void UtcNullableDateTimeConverter_Read_Value_ParsesAsUtc()
		{
			using var reader = ReadSingleRow("2023-08-07");

			var result = new UtcNullableDateTimeConverter().Read(reader, 0);

			result!.Value.ToUniversalTime().Should().Be(new DateTime(2023, 8, 7, 0, 0, 0, DateTimeKind.Utc));
		}
	}
}
