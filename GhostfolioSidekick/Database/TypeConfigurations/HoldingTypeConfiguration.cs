using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
    internal class HoldingTypeConfiguration : IEntityTypeConfiguration<Holding>
    {
        private readonly ValueComparer<ICollection<PartialSymbolIdentifier>> partialSymbolIdentifiersListComparer;
        private readonly JsonSerializerOptions serializationOptions;
        private const string PartialSymbolIdentifiers = "PartialSymbolIdentifiers";

        public HoldingTypeConfiguration()
        {
            partialSymbolIdentifiersListComparer = new ValueComparer<ICollection<PartialSymbolIdentifier>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var stringEnumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
            serializationOptions = new JsonSerializerOptions();
            serializationOptions.Converters.Add(stringEnumConverter);
        }

        public void Configure(EntityTypeBuilder<Holding> builder)
        {
            builder.ToTable("Holdings");
            builder.HasKey(psi => psi.Id);

            builder.Property(b => b.PartialSymbolIdentifiers)
                .HasColumnName(PartialSymbolIdentifiers)
                .HasConversion(
                    v => PartialSymbolIdentifiersToString(v),
                    v => StringToPartialSymbolIdentifiers(v),
                    partialSymbolIdentifiersListComparer);

            builder.Property(b => b.TWR)
                .HasColumnName("TWR")
                .HasPrecision(18, 8);

            builder.Property(b => b.AverageBuyPrice)
                .HasColumnName("AverageBuyPrice")
                .HasPrecision(18, 8);
        }

        private IList<PartialSymbolIdentifier> StringToPartialSymbolIdentifiers(string v)
        {
            return JsonSerializer.Deserialize<ICollection<PartialSymbolIdentifier>>(v, serializationOptions)?.ToList() ?? [];
        }

        private string PartialSymbolIdentifiersToString(ICollection<PartialSymbolIdentifier> v)
        {
            return JsonSerializer.Serialize(v, serializationOptions);
        }
    }
}
