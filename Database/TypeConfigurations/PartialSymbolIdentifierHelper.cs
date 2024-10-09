using GhostfolioSidekick.Model.Activities;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal static class PartialSymbolIdentifierHelper
	{
		internal static string PartialSymbolIdentifiersToString(ICollection<PartialSymbolIdentifier> v)
		{
			return JsonSerializer.Serialize(v);
		}

		internal static ICollection<PartialSymbolIdentifier> StringToPartialSymbolIdentifiers(string v)
		{
			return JsonSerializer.Deserialize<ICollection<PartialSymbolIdentifier>>(v) ?? [];
		}
	}
}