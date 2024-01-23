namespace GhostfolioSidekick.GhostfolioAPI
{
	internal static class TransactionReferenceUtilities
	{
		internal static string GetComment(string? transactionId, string? assetId)
		{
			if (string.IsNullOrWhiteSpace(transactionId))
			{
				throw new NotSupportedException();
			}

			return $"Transaction Reference: [{transactionId}] (Details: asset {assetId ?? "<EMPTY>"})";
		}

		internal static string GetComment(string? transactionId)
		{
			if (string.IsNullOrWhiteSpace(transactionId))
			{
				throw new NotSupportedException();
			}

			return $"Transaction Reference: [{transactionId}]";
		}
	}
}
