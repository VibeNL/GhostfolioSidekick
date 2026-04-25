using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class TaskRunTypeConfiguration : IEntityTypeConfiguration<TaskRun>
	{
		private static readonly ValueConverter<DateTimeOffset, string> DateTimeOffsetConverter =
			new(
				v => v.ToString("O", CultureInfo.InvariantCulture),
				v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

		private static readonly ValueConverter<DateTimeOffset?, string?> NullableDateTimeOffsetConverter =
			new(
				v => v.HasValue ? v.Value.ToString("O", CultureInfo.InvariantCulture) : null,
				v => v != null ? DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) : null);

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
