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
				if (string.IsNullOrWhiteSpace(identifier))
				{
					continue;
				}

				foreach (var candidate in candidateValues)
				{
					if (string.IsNullOrWhiteSpace(candidate))
					{
						continue;
					}

					scores.Add(GetMatchScore(identifier, candidate));
				}
			}

			return scores.Count > 0 ? scores.Max() : 0;	
		}

		private static int GetMatchScore(string identifier, string candidate)
		{
			if (string.Equals(identifier, candidate, StringComparison.InvariantCultureIgnoreCase))
			{
				return 100;
			}
			if (candidate.Contains(identifier, StringComparison.InvariantCultureIgnoreCase))
			{
				return 80;
			}
			return CalculateFuzzyScore(identifier, candidate);
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
