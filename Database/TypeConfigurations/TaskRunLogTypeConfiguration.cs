using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal partial class TaskRunLogTypeConfiguration : IEntityTypeConfiguration<TaskRunLog>
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

		public void Configure(EntityTypeBuilder<TaskRunLog> builder)
		{
			builder.ToTable("TaskRunLogs");
			builder.HasKey(l => l.Id);
			builder.Property(l => l.Message).IsRequired();
			builder.Property(l => l.TaskRunType).IsRequired();
			builder.Property(l => l.Timestamp).HasConversion(DateTimeOffsetConverter);
			builder.HasOne(l => l.TaskRun)
				.WithMany(t => t.Logs)
				.HasForeignKey(l => l.TaskRunType)
				.HasPrincipalKey(t => t.Type)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
