using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal partial class TaskRunTypeConfiguration : IEntityTypeConfiguration<TaskRun>
	{
		// Truncates fractional seconds to at most 6 digits; WASM runtime cannot parse 7-digit fractions
		[GeneratedRegex(@"(\.\d{6})\d+")]
		private static partial Regex FractionalSecondsRegex();

       private static DateTimeOffset ParseSafe(string v)
		{
			if (string.IsNullOrWhiteSpace(v))
			{
				return DateTimeOffset.MinValue;
			}
			try
			{
				var normalized = FractionalSecondsRegex().Replace(v.Trim(), "$1");
				return DateTimeOffset.Parse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None);
			}
			catch (Exception ex)
			{
				throw new FormatException($"Failed to parse DateTimeOffset value: '{v}'", ex);
			}
		}

		private static readonly ValueConverter<DateTimeOffset, string> DateTimeOffsetConverter =
			new(
				v => v.ToString("O", CultureInfo.InvariantCulture),
				v => ParseSafe(v));

		private static readonly ValueConverter<DateTimeOffset?, string?> NullableDateTimeOffsetConverter =
			new(
				v => v.HasValue ? v.Value.ToString("O", CultureInfo.InvariantCulture) : null,
				v => v != null ? ParseSafe(v) : null);

		public void Configure(EntityTypeBuilder<TaskRun> builder)
		{
			builder.ToTable("TaskRuns");
			builder.HasKey(a => a.Type);

			builder.Property(a => a.LastUpdate).HasConversion(DateTimeOffsetConverter);
			builder.Property(a => a.NextSchedule).HasConversion(DateTimeOffsetConverter);
			builder.Property(a => a.StartTime).HasConversion(NullableDateTimeOffsetConverter);
			builder.Property(a => a.EndTime).HasConversion(NullableDateTimeOffsetConverter);
		}
	}
}
