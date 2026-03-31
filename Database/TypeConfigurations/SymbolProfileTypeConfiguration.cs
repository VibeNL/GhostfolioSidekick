using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class SymbolProfileTypeConfiguration : IEntityTypeConfiguration<SymbolProfile>
	{
		public void Configure(EntityTypeBuilder<SymbolProfile> builder)
		{
			builder.ToTable("SymbolProfiles");
			builder.HasKey(x => new { x.Symbol, x.DataSource });

			builder.ComplexProperty(b => b.Currency).Property(p => p.Symbol).HasColumnName("Currency");

			builder.Property(e => e.Identifiers)
						.HasConversion(
							v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
							v => DeserializeIdentifiers(v),
							new ValueComparer<List<SymbolIdentifier>>(
								(c1, c2) => c1!.SequenceEqual(c2!),
								c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
								c => c.ToList()));

			builder.Property(e => e.SectorWeights)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<SectorWeight>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<SectorWeight>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<SectorWeight>)c.ToList()));

			builder.Property(e => e.CountryWeight)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<CountryWeight>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<CountryWeight>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<CountryWeight>)c.ToList()));

			builder.Property(x => x.AssetClass).HasConversion<string>();
			builder.Property(x => x.AssetSubClass).HasConversion<string>();

			builder.HasMany(x => x.MarketData).WithOne().OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.StockSplits).WithOne().OnDelete(DeleteBehavior.Cascade);
		}

		private static List<SymbolIdentifier> DeserializeIdentifiers(string json)
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (root.ValueKind == JsonValueKind.Array
				&& root.GetArrayLength() > 0
				&& root[0].ValueKind == JsonValueKind.String)
			{
				// Old format: array of plain strings - convert to SymbolIdentifier with Default type
				return [.. root.EnumerateArray()
					.Select(e => new SymbolIdentifier { Identifier = e.GetString()!, IdentifierType = IdentifierType.Default })];
			}

			return JsonSerializer.Deserialize<List<SymbolIdentifier>>(json, (JsonSerializerOptions)null!) ?? [];
		}
	}
}
