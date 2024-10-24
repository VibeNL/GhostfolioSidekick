using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Matches;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivitySymbolTypeConfiguration : IEntityTypeConfiguration<ActivitySymbol>
	{
		private readonly JsonSerializerOptions serializationOptions;
		private const string PartialSymbolIdentifiers = "PartialSymbolIdentifiers";

		public ActivitySymbolTypeConfiguration()
		{
			var stringEnumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
			serializationOptions = new JsonSerializerOptions();
			serializationOptions.Converters.Add(stringEnumConverter);
		}

		public void Configure(EntityTypeBuilder<ActivitySymbol> builder)
		{
			builder.ToTable("ActivitySymbol");
			builder.HasKey(psi => psi.Id);

			builder.Property(b => b.PartialSymbolIdentifier)
				.HasColumnName(PartialSymbolIdentifiers)
				.HasConversion(
					v => PartialSymbolIdentifiersToString(v),
					v => StringToPartialSymbolIdentifiers(v));
		}

		private PartialSymbolIdentifier StringToPartialSymbolIdentifiers(string v)
		{
			return JsonSerializer.Deserialize<PartialSymbolIdentifier>(v, serializationOptions) ?? throw new NotSupportedException();
		}

		private string PartialSymbolIdentifiersToString(PartialSymbolIdentifier v)
		{
			return JsonSerializer.Serialize(v, serializationOptions);
		}
	}
}
