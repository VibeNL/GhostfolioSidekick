using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class TaskRunLogTypeConfiguration : IEntityTypeConfiguration<TaskRunLog>
	{
		private static readonly ValueConverter<DateTimeOffset, string> DateTimeOffsetConverter =
			new(
				v => v.ToString("O", CultureInfo.InvariantCulture),
				v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

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
