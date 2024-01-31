using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	internal static class TransactionReferenceUtilities
	{
		internal static string GetComment(Model.Activities.Activity activity, SymbolProfile? symbolProfile)
		{
			if (string.IsNullOrWhiteSpace(activity.TransactionId))
			{
				throw new NotSupportedException();
			}

			return $"Transaction Reference: [{activity.TransactionId}] (Details: asset {symbolProfile?.Symbol ?? "<EMPTY>"})";
		}

		internal static string GetComment(Model.Activities.Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.TransactionId))
			{
				throw new NotSupportedException();
			}

			return $"Transaction Reference: [{activity.TransactionId}]";
		}
	}
}
