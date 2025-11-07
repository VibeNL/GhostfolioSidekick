using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class TaskRunTypeConfiguration : IEntityTypeConfiguration<TaskRun>
	{
		public void Configure(EntityTypeBuilder<TaskRun> builder)
		{
			builder.ToTable("TaskRuns");
			builder.HasKey(a => a.Type);
		}
	}
}
