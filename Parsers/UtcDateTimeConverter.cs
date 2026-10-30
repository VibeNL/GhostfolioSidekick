using CsvHelper;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GhostfolioSidekick.Parsers
{
	public sealed class UtcDateTimeConverter : ICsvConverter<DateTime>
	{
		private static readonly string[] Formats =
		[
			"yyyy-MM-ddTHH:mm:ssZ",
			"yyyy-MM-dd HH:mm:ss UTC",
			"yyyy-MM-dd HH:mm UTC",
			"yyyy-MM-dd HH:mm:ss",
			"yyyy-MM-dd",
		];

		public DateTime Read(CsvReader reader, int index)
		{
			return Parse(reader.GetString(index));
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, DateTime value)
		{
			writer.WriteField(value.ToString("o", CultureInfo.InvariantCulture));
		}

		internal static DateTime Parse(string text)
		{
			if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exact))
			{
				return exact;
			}

			return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
		}
	}

	public sealed class DayFirstUtcDateTimeConverter : ICsvConverter<DateTime>
	{
		private static readonly string[] Formats =
		[
			"dd/MM/yyyy",
			"dd-MM-yyyy",
		];

		public DateTime Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exact))
			{
				return exact;
			}

			return UtcDateTimeConverter.Parse(text);
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, DateTime value)
		{
			writer.WriteField(value.ToString("o", CultureInfo.InvariantCulture));
		}
	}

	public sealed class UtcNullableDateTimeConverter : ICsvConverter<DateTime?>
	{
		public DateTime? Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			return string.IsNullOrWhiteSpace(text) ? null : UtcDateTimeConverter.Parse(text);
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, DateTime? value)
		{
			writer.WriteField(value?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty);
		}
	}
}
