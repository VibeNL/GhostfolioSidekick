using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Xml.Linq;

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

			builder	.ComplexProperty(x => x.Currency)
					.Property(x => x.Symbol).HasColumnName("Currency");

            // Configure the list properties to be stored as JSON strings with sorted lists for canonical representation
           var assetClassComparer = new ValueComparer<List<AssetClass>>(
				(a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
				a => a == null ? 0 : a.Aggregate(0, (acc, v) => HashCode.Combine(acc, v.GetHashCode())),
				a => a == null ? new List<AssetClass>() : a.ToList()
			);
			var allowedAssetClassesProp = builder.Property(e => e.AllowedAssetClasses)
				.HasConversion(
					v => v == null ? null : JsonSerializer.Serialize(v.OrderBy(x => x).ToList(), (JsonSerializerOptions?)null),
					v => v == null ? null : JsonSerializer.Deserialize<List<AssetClass>>(v, (JsonSerializerOptions?)null));
			allowedAssetClassesProp.Metadata.SetValueComparer(assetClassComparer);

         var assetSubClassComparer = new ValueComparer<List<AssetSubClass>>(
				(a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
				a => a == null ? 0 : a.Aggregate(0, (acc, v) => HashCode.Combine(acc, v.GetHashCode())),
				a => a == null ? new List<AssetSubClass>() : a.ToList()
			);
			var allowedAssetSubClassesProp = builder.Property(e => e.AllowedAssetSubClasses)
				.HasConversion(
					v => v == null ? null : JsonSerializer.Serialize(v.OrderBy(x => x).ToList(), (JsonSerializerOptions?)null),
					v => v == null ? null : JsonSerializer.Deserialize<List<AssetSubClass>>(v, (JsonSerializerOptions?)null));
			allowedAssetSubClassesProp.Metadata.SetValueComparer(assetSubClassComparer);

			// Add Unique index on Identifier, AllowedAssetClasses and AllowedAssetSubClasses
			builder.HasIndex(p => new { p.Identifier, p.AllowedAssetClasses, p.AllowedAssetSubClasses })
				.IsUnique()
				.HasDatabaseName("IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass");
		}
	}
}
