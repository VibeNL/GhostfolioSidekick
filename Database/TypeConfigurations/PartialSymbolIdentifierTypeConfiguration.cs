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
		private readonly JsonSerializerOptions opts;

		public PartialSymbolIdentifierTypeConfiguration()
		{
			var stringEnumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
			opts = new JsonSerializerOptions();
			opts.Converters.Add(stringEnumConverter);
		}

		public void Configure(EntityTypeBuilder<PartialSymbolIdentifier> builder)
		{
			builder.ToTable("PartialSymbolIdentifiers");
			builder.HasKey(psi => psi.Id);

			builder.Property(e => e.AllowedAssetClasses)
					.HasConversion(
						v => JsonSerializer.Serialize(v, opts),
						v => JsonSerializer.Deserialize<List<AssetClass>>(v, opts),
						new ValueComparer<ICollection<AssetClass>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<AssetClass>)c.ToList()));
			builder.Property(e => e.AllowedAssetSubClasses)
					.HasConversion(
						v => JsonSerializer.Serialize(v, opts),
						v => JsonSerializer.Deserialize<List<AssetSubClass>>(v, opts),
						new ValueComparer<ICollection<AssetSubClass>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<AssetSubClass>)c.ToList()));

			builder.HasIndex(psi => new { psi.Identifier, psi.AllowedAssetClasses, psi.AllowedAssetSubClasses }).IsUnique();

			//builder.HasMany(psi => psi.SymbolProfiles)
			//		.WithMany(sp => sp.MatchedPartialIdentifiers)
			//		.UsingEntity(
			//			"MatchedPartialIdentifiers",
			//			l => l.HasOne(typeof(SymbolProfile)).WithMany().HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource").IsRequired(false).HasPrincipalKey(nameof(SymbolProfile.Symbol), nameof(SymbolProfile.DataSource)),
			//			r => r.HasOne(typeof(PartialSymbolIdentifier)).WithMany().HasForeignKey("PartialIdentifierId").IsRequired(false).HasPrincipalKey(nameof(PartialSymbolIdentifier.Id)),
			//			j => j.HasKey("SymbolProfileSymbol", "SymbolProfileDataSource", "PartialIdentifierId"));

			//builder.HasMany(psi => psi.Activities)
			//		.WithMany(nameof(IActivityWithPartialIdentifier.PartialSymbolIdentifiers))
			//		.UsingEntity(
			//			"ActivityPartialIdentifiers",
			//			l => l.HasOne(typeof(Activity)).WithMany().HasForeignKey("ActivityId").IsRequired(false).HasPrincipalKey(nameof(Activity.Id)),
			//			r => r.HasOne(typeof(PartialSymbolIdentifier)).WithMany().HasForeignKey("PartialIdentifierId").IsRequired(false).HasPrincipalKey(nameof(PartialSymbolIdentifier.Id)),
			//			j => j.HasKey("ActivityId", "PartialIdentifierId"));
		}
	}
}
