using System;
using Xunit;
using GhostfolioSidekick.Utilities;

namespace GhostfolioSidekick.Utilities.UnitTests
{
	public class SemanticMatcherTests
	{
		[Fact]
		public void CalculateSemanticMatchScore_ExactMatch_Returns100()
		{
			var source = new[] { "AAPL" };
			var candidates = new[] { "AAPL" };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.Equal(100, score);
		}

		[Fact]
		public void CalculateSemanticMatchScore_PartialMatch_Returns80()
		{
			var source = new[] { "AAPL" };
			var candidates = new[] { "AAPL Inc." };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.Equal(80, score);
		}

		[Fact]
		public void CalculateSemanticMatchScore_FuzzyMatch_ReturnsBetween1And70()
		{
			var source = new[] { "AAPL" };
			var candidates = new[] { "AAP" };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.InRange(score, 1, 70);
		}

		[Fact]
		public void CalculateSemanticMatchScore_CaseInsensitiveMatch_Returns100()
		{
			var source = new[] { "aapl" };
			var candidates = new[] { "AAPL" };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.Equal(100, score);
		}

		[Fact]
		public void CalculateSemanticMatchScore_NullOrEmptySource_Returns0()
		{
			int score1 = SemanticMatcher.CalculateSemanticMatchScore(null!, ["AAPL"]);
			int score2 = SemanticMatcher.CalculateSemanticMatchScore([], ["AAPL"]);
			Assert.Equal(0, score1);
			Assert.Equal(0, score2);
		}

		[Fact]
		public void CalculateSemanticMatchScore_NullOrEmptyCandidates_Returns0()
		{
			int score1 = SemanticMatcher.CalculateSemanticMatchScore(["AAPL"], null!);
			int score2 = SemanticMatcher.CalculateSemanticMatchScore(["AAPL"], []);
			Assert.Equal(0, score1);
			Assert.Equal(0, score2);
		}

		[Fact]
		public void CalculateSemanticMatchScore_WhitespaceValues_Ignored()
		{
			var source = new[] { " ", "AAPL" };
			var candidates = new[] { "AAPL" };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.Equal(100, score);
		}

		[Fact]
		public void CalculateSemanticMatchScore_MultipleCandidates_ReturnsMaxScore()
		{
			var source = new[] { "AAPL" };
			var candidates = new[] { "AAP", "AAPL Inc.", "AAPL" };
			int score = SemanticMatcher.CalculateSemanticMatchScore(source, candidates);
			Assert.Equal(100, score);
		}
	}
}
