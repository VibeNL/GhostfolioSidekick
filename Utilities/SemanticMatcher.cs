using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fastenshtein;

namespace GhostfolioSidekick.Utilities
{
	public static class SemanticMatcher
	{
		/// <summary>
		/// Calculates a semantic match score between a set of symbol identifiers and candidate values.
		/// </summary>
		/// <param name="sourceValues">Array of strings (usually ticker, name, etc)</param>
		/// <param name="candidateValues">Array of candidate strings to match against (e.g. ticker, name)</param>
		/// <returns>Score between 0 and 100 (higher is better)</returns>
		public static int CalculateSemanticMatchScore(
			string[] sourceValues,
			string[] candidateValues)
		{
			if (sourceValues == null || candidateValues == null || candidateValues.Length == 0)
			{
				return 0;
			}

			var scores = new List<int>();
			foreach (var identifier in sourceValues)
			{
				foreach (var candidate in candidateValues)
				{
					if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(candidate))
						continue;

					// Case-insensitive exact match
					if (string.Equals(identifier, candidate, StringComparison.InvariantCultureIgnoreCase))
					{
						scores.Add(100);
						continue;
					}

					// Partial match (substring)
					if (candidate.Contains(identifier, StringComparison.InvariantCultureIgnoreCase))
					{
						scores.Add(80);
						continue;
					}

					// Fuzzy match: Levenshtein distance based similarity
					int fuzzyScore = CalculateFuzzyScore(identifier, candidate);
					scores.Add(fuzzyScore);
				}
			}

			return scores.Count > 0 ? scores.Max() : 0;
		}

		// Simple Levenshtein-based similarity (returns 0-70)
		private static int CalculateFuzzyScore(string a, string b)
		{
			var levenshtein = new Levenshtein(a);
			int distance = levenshtein.DistanceFrom(b);
			int maxLen = Math.Max(a.Length, b.Length);
			if (maxLen == 0)
			{
				return 0;
			}

			double similarity = 1.0 - (double)distance / maxLen;
			return (int)(similarity * 70); // scale to 0-70
		}
	}
}
