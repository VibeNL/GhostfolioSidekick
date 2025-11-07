using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class TaskRunLogTypeConfiguration : IEntityTypeConfiguration<TaskRunLog>
	{
		public void Configure(EntityTypeBuilder<TaskRunLog> builder)
		{
			builder.ToTable("TaskRunLogs");
			builder.HasKey(l => l.Id);
			builder.Property(l => l.Message).IsRequired();
			builder.Property(l => l.TaskRunType).IsRequired();
			builder.HasOne(l => l.TaskRun)
				.WithMany(t => t.Logs)
				.HasForeignKey(l => l.TaskRunType)
				.HasPrincipalKey(t => t.Type)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
