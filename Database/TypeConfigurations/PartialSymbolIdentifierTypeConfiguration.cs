using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

			// Configure the list properties to be stored as JSON strings with sorted lists for canonical representation
			builder.Property(e => e.AllowedAssetClasses)
			 .HasConversion(
					v => JsonSerializer.Serialize((v ?? new List<AssetClass>()).OrderBy(x => x).ToList(), (JsonSerializerOptions?)null),
					v => string.IsNullOrEmpty(v) ? new List<AssetClass>() : JsonSerializer.Deserialize<List<AssetClass>>(v, (JsonSerializerOptions?)null) ?? new List<AssetClass>())
				.Metadata.SetValueComparer(new ValueComparer<List<AssetClass>>(
					(c1, c2) => c1 != null && c2 != null && c1.OrderBy(x => x).SequenceEqual(c2.OrderBy(x => x)),
					c => c.OrderBy(x => x).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
					c => c.ToList()));

			builder.Property(e => e.AllowedAssetSubClasses)
				 .HasConversion(
						v => JsonSerializer.Serialize((v ?? new List<AssetSubClass>()).OrderBy(x => x).ToList(), (JsonSerializerOptions?)null),
						v => string.IsNullOrEmpty(v) ? new List<AssetSubClass>() : JsonSerializer.Deserialize<List<AssetSubClass>>(v, (JsonSerializerOptions?)null) ?? new List<AssetSubClass>())
				.Metadata.SetValueComparer(new ValueComparer<List<AssetSubClass>>(
					(c1, c2) => c1 != null && c2 != null && c1.OrderBy(x => x).SequenceEqual(c2.OrderBy(x => x)),
					c => c.OrderBy(x => x).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
					c => c.ToList()));

			builder.Property(e => e.Currency)
				.HasConversion(
					v => v == null ? null : v.Symbol,
					v => v == null ? null : Currency.GetCurrency(v));

			// Add Unique index
			builder.HasIndex(p => new { p.IdentifierType, p.Identifier, p.Currency, p.AllowedAssetClasses, p.AllowedAssetSubClasses })
				.IsUnique()
				.HasDatabaseName("IX_PartialSymbolIdentifiers_IdentifierType_Identifier_Currency_AllowedAssetClass_AllowedAssetSubClass");
		}
	}
}
