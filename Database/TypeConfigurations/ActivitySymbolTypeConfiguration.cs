using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Matches;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivitySymbolTypeConfiguration : IEntityTypeConfiguration<ActivitySymbol>
	{
		private readonly JsonSerializerOptions opts;

		public void Configure(EntityTypeBuilder<ActivitySymbol> builder)
		{
			builder.ToTable("ActivitySymbol");
			builder.HasKey(psi => psi.Id);
		}
	}
}
