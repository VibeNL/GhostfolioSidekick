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

			// Add Unique index on Identifier, AllowedAssetClasses and AllowedAssetSubClasses
			builder.HasIndex(p => new { p.Identifier, p.AllowedAssetClasses, p.AllowedAssetSubClasses })
				.IsUnique()
				.HasDatabaseName("IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass");
		}
	}
}
