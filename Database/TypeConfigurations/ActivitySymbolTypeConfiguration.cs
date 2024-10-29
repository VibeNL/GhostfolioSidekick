using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class HoldingTypeConfiguration : IEntityTypeConfiguration<Holding>
	{
		public void Configure(EntityTypeBuilder<Holding> builder)
		{
			builder.ToTable("Holdings");
			builder.HasKey(psi => psi.Id);

		}
	}
}
