using GhostfolioSidekick.Model;
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

			builder.OwnsOne<Currency>(b => b.Currency, m =>
			{
				m.Property(p => p.Symbol).HasColumnName("Currency");
			});
			builder.Property(e => e.Identifiers)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<string>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<string>)c.ToList()));

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
	}
}
