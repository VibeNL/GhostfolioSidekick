using AwesomeAssertions;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Yahoo
{
	public class AdrRatioParserTests
	{
		[Theory]
		[InlineData("GDR (EACH REP 25 COM STK KRW100)", 25)]
		[InlineData("GDR (EACH REPRESENTING 4 ORD SHS)", 4)]
		[InlineData("one ADR represents 4 ordinary shares", 4)]
		[InlineData("Each ADS represents 0.5 of one ordinary share", 0.5)]
		[InlineData("ADR ratio of 10:1", 10)]
		[InlineData("GDR ratio: 2:1", 2)]
		[InlineData("The security represents 25 ordinary shares of the issuer", 25)]
		public void TryParseSharesPerReceipt_ShouldExtractRatio_ForKnownPhrasings(string text, decimal expected)
		{
			// Act
			var result = AdrRatioParser.TryParseSharesPerReceipt(text);

			// Assert
			result.Should().Be(expected);
		}

		[Fact]
		public void TryParseSharesPerReceipt_ShouldReturnNull_WhenNoTextMatches()
		{
			// Act
			var result = AdrRatioParser.TryParseSharesPerReceipt("Apple Inc. designs, manufactures and markets smartphones.");

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public void TryParseSharesPerReceipt_ShouldReturnNull_WhenAllFragmentsAreEmpty()
		{
			// Act
			var result = AdrRatioParser.TryParseSharesPerReceipt(null, string.Empty, "   ");

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public void TryParseSharesPerReceipt_ShouldCheckLaterFragments_WhenEarlierOnesDoNotMatch()
		{
			// Act
			var result = AdrRatioParser.TryParseSharesPerReceipt("Samsung Electronics", "GDR (EACH REP 25 COM STK KRW100)");

			// Assert
			result.Should().Be(25);
		}
	}
}
