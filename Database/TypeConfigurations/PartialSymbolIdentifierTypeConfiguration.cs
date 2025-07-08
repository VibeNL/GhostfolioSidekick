using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

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

			 // Configure the list properties to be stored as JSON strings
			builder.Property(e => e.AllowedAssetClasses)
				.HasConversion(
					v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => v == null ? null : JsonSerializer.Deserialize<List<AssetClass>>(v, (JsonSerializerOptions?)null));

			builder.Property(e => e.AllowedAssetSubClasses)
				.HasConversion(
					v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => v == null ? null : JsonSerializer.Deserialize<List<AssetSubClass>>(v, (JsonSerializerOptions?)null));

			// Add Unique index on Identifier, AllowedAssetClasses and AllowedAssetSubClasses
			builder.HasIndex(p => new { p.Identifier, p.AllowedAssetClasses, p.AllowedAssetSubClasses })
				.IsUnique()
				.HasDatabaseName("IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass");
		}
	}
}
