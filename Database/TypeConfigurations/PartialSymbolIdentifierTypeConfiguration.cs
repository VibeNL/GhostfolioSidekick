using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class PartialSymbolIdentifierTypeConfiguration : IEntityTypeConfiguration<PartialSymbolIdentifier>
	{
		public void Configure(EntityTypeBuilder<PartialSymbolIdentifier> builder)
		{
			builder.ToTable("PartialSymbolIdentifiers");
			builder.Property<long>("ID")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);
		}
	}
}
