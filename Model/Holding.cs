using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model
{
	public class Holding
	{
		public int Id { get; set; }

		public virtual List<SymbolProfile> SymbolProfiles { get; set; } = [];

		public virtual List<Activity> Activities { get; set; } = [];

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public void MergeIdentifiers(IList<PartialSymbolIdentifier> ids)
		{
			foreach (var item in ids.Where(item => !IdentifierContainsInList(item)))
			{
				PartialSymbolIdentifiers.Add(item);
			}
		}

		[ExcludeFromCodeCoverage]
		override public string ToString()
		{
			return $"{SymbolProfiles.FirstOrDefault()?.Symbol} - {Activities.Count} activities";
		}

		public bool IdentifierContainsInList(PartialSymbolIdentifier newId)
		{
			foreach (var item in PartialSymbolIdentifiers)
			{
				var idsMatch = item.Identifier == newId.Identifier;
				var allowedAssetClassesMatch =
					IsEmpty(item.AllowedAssetClasses) ||
					IsEmpty(newId.AllowedAssetClasses) ||
					(item.AllowedAssetClasses?.Any(y => newId.AllowedAssetClasses?.Contains(y) ?? true) ?? true);
				var allowedSubClass =
					IsEmpty(item.AllowedAssetSubClasses) ||
					IsEmpty(newId.AllowedAssetSubClasses) ||
					(item.AllowedAssetSubClasses?.Any(y => newId.AllowedAssetSubClasses?.Contains(y) ?? true) ?? true);

				if (idsMatch && allowedAssetClassesMatch && allowedSubClass)
				{
					return true;
				}
			}

			return false;
		}

		public bool HasPartialSymbolIdentifier(IList<PartialSymbolIdentifier> partialIdentifiers)
		{
			return partialIdentifiers.Any(IdentifierContainsInList);
		}

		private static bool IsEmpty<T>(List<T>? list)
		{
			if (list == null)
			{
				return true;
			}

			return list.Count == 0;
		}
	}
}
