//using GhostfolioSidekick.Model.Symbols;
//using System.Text.RegularExpressions;

//namespace GhostfolioSidekick.GhostfolioAPI
//{
//	internal static class TransactionReferenceUtilities
//	{
//		internal static string GetComment(Model.Activities.IActivity activity, SymbolProfile? symbolProfile)
//		{
//			if (string.IsNullOrWhiteSpace(activity.TransactionId))
//			{
//				return string.Empty;
//			}

//			return $"Transaction Reference: [{activity.TransactionId}] (Details: asset {symbolProfile?.Symbol ?? "<EMPTY>"})";
//		}

//		internal static string GetComment(Model.Activities.IActivity activity)
//		{
//			if (string.IsNullOrWhiteSpace(activity.TransactionId))
//			{
//				return string.Empty;
//			}

//			return $"Transaction Reference: [{activity.TransactionId}]";
//		}

//		internal static string? ParseComment(Contract.Activity activity)
//		{
//			var comment = activity.Comment;
//			if (string.IsNullOrWhiteSpace(comment))
//			{
//				return null;
//			}

//			var pattern = @"Transaction Reference: \[(.*?)\]";
//			var match = Regex.Match(comment, pattern);
//			var key = match.Groups.Count > 1 ? match.Groups[1].Value : null;
//			return key ?? string.Empty;
//		}
//	}
//}
