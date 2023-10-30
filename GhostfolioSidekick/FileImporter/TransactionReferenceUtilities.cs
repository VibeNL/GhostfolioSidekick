namespace GhostfolioSidekick.FileImporter
{
	internal static class TransactionReferenceUtilities
	{
		internal static string GetComment(string transactionId, string? assetId)
		{
			return $"Transaction Reference: [{transactionId}] (Details: asset {assetId ?? "<EMPTY>"})";
		}

		internal static string GetComment(string id)
		{
			return $"Transaction Reference: [{id}]";
		}
	}
}
