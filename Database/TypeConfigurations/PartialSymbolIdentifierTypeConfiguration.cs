using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
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
			builder.HasKey(psi => psi.Id);

			builder.Property(e => e.AllowedAssetClasses)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<AssetClass>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<AssetClass>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<AssetClass>)c.ToList()));
			builder.Property(e => e.AllowedAssetSubClasses)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<AssetSubClass>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<AssetSubClass>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<AssetSubClass>)c.ToList()));

			builder.HasIndex(psi => new { psi.Identifier, psi.AllowedAssetClasses, psi.AllowedAssetSubClasses }).IsUnique();

			builder.HasMany(psi => psi.SymbolProfiles)
					.WithMany(sp => sp.MatchedPartialIdentifiers)
					.UsingEntity(
						"MatchedPartialIdentifiers",
						l => l.HasOne(typeof(SymbolProfile)).WithMany().HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource").HasPrincipalKey(nameof(SymbolProfile.Symbol), nameof(SymbolProfile.DataSource)),
						r => r.HasOne(typeof(PartialSymbolIdentifier)).WithMany().HasForeignKey("PartialIdentifierId").HasPrincipalKey(nameof(PartialSymbolIdentifier.Id)),
						j => j.HasKey("SymbolProfileSymbol", "SymbolProfileDataSource", "PartialIdentifierId"));

			builder.HasMany(psi => psi.Activities)
					.WithMany(nameof(IActivityWithPartialIdentifier.PartialSymbolIdentifiers))
					.UsingEntity(
						"ActivityPartialIdentifiers",
						l => l.HasOne(typeof(Activity)).WithMany().HasForeignKey("ActivityId").IsRequired(false).HasPrincipalKey(nameof(Activity.Id)),
						r => r.HasOne(typeof(PartialSymbolIdentifier)).WithMany().HasForeignKey("PartialIdentifierId").HasPrincipalKey(nameof(PartialSymbolIdentifier.Id)),
						j => j.HasKey("ActivityId", "PartialIdentifierId"));
		}
	}
}
