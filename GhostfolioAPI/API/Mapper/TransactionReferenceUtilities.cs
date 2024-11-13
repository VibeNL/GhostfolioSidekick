using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	internal static class TransactionReferenceUtilities
	{
		internal static string GetComment(Model.Activities.Activity activity, Contract.SymbolProfile? symbolProfile)
		{
			if (string.IsNullOrWhiteSpace(activity.TransactionId))
			{
				return string.Empty;
			}

			return $"Transaction Reference: [{activity.TransactionId}] (Details: asset {symbolProfile?.Symbol ?? "<EMPTY>"})";
		}

		internal static string GetComment(Model.Activities.Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.TransactionId))
			{
				return string.Empty;
			}

			return $"Transaction Reference: [{activity.TransactionId}]";
		}
	}
}
