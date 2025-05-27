using AwesomeAssertions;
using GhostfolioSidekick.Model.ISIN;

namespace GhostfolioSidekick.Model.UnitTests.ISIN
{
	public class IsinTests
	{
		[Theory]
		[InlineData("US0378331005")]     // Apple
		[InlineData("AU0000XVGZA3")]     // Treasury Corporation of Victoria
		[InlineData("GB0002634946")]     // BAE Systems
		[InlineData("US30303M1027")]     // Meta (Facebook)
		[InlineData("US02079K1079")]     // Google Class C
		[InlineData("GB0031348658")]     // Barclays
		[InlineData("US88160R1014")]     // Tesla
		public void ValidateCheckDigit_ShouldReturnTrue_WhenValueContainsValidCheckDigit(string value)
	  => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Theory]                         // Dummy values created using https://www.isindb.com/fix-isin-calculate-isin-check-digit/
		[InlineData("AU0000VXGZA3")]     // AU0000XVGZA3 with two character transposition XV -> VX
		[InlineData("US0000000QB4")]     // US0000000BQ4 with two character transposition BQ -> QB
		[InlineData("GB123909ABC8")]     // GB123099ABC8 with two digit transposition 09 -> 90
		[InlineData("GB8091XYZ349")]     // GB8901XYZ349 with two digit transposition 90 -> 09
		[InlineData("US1155334451")]     // US1122334451 with two digit twin error 22 -> 55
		[InlineData("US1122337751")]     // US1122334451 with two digit twin error 44 -> 77
		[InlineData("US9988773340")]     // US9988776640 with two digit twin error 66 -> 33
		[InlineData("US3030M31027")]     // US30303M1027 with two character transposition 3M -> M3
		[InlineData("US303031M027")]     // US30303M1027 with two character transposition M1 -> 1M
		[InlineData("AU000X0VGZA3")]     // AU0000XVGZA3 with two character transposition 0X -> X0
		[InlineData("G0B002634946")]     // GB0002634946 with two character transposition B0 -> 0B
		public void ValidateCheckDigit_ShouldReturnTrue_WhenValueContainsUndetectableError(string value)
		   => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Theory]
		[InlineData("US30703M1027")]     // US30303M1027 with single digit transcription error 3 -> 7
		[InlineData("US02079J1079")]     // US02079K1079 with single character transcription error K -> J
		[InlineData("GB0031338658")]     // GB0031348658 with single digit transcription error 4 -> 3
		[InlineData("US0387331005")]     // US0378331005 with two digit transposition error 78 -> 87 
		[InlineData("US020791K079")]     // US02079K1079 with two character transposition error K1 -> 1K
		[InlineData("US99160R1014")]     // US88160R1014 with two digit twin error 88 -> 99
		[InlineData("GB0112634946")]     // GB0002634946 with two digit twin error 00 -> 11
		[InlineData("US12BB3DD566")]     // US12AA3DD566 with two letter twin error AA -> BB
		public void ValidateCheckDigit_ShouldReturnFalse_WhenValueContainsDetectableError(string value)
		   => Isin.ValidateCheckDigit(value).Should().BeFalse();


		[Theory]
		[InlineData("000000000018")]
		[InlineData("000000001008")]
		[InlineData("000000100008")]
		[InlineData("000010000008")]
		[InlineData("001000000008")]
		[InlineData("100000000008")]
		public void ValidateCheckDigit_ShouldCorrectlyWeightOddPositionDigits(String value)
		   => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Theory]
		[InlineData("000000000109")]
		[InlineData("000000010009")]
		[InlineData("000001000009")]
		[InlineData("000100000009")]
		[InlineData("010000000009")]
		public void ValidateCheckDigit_ShouldCorrectlyWeightEvenPositionDigits(String value)
		   => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Theory]
		[InlineData("0000000000A9")]
		[InlineData("00000000A009")]
		[InlineData("000000A00009")]
		[InlineData("0000A0000009")]
		[InlineData("00A000000009")]
		[InlineData("A00000000009")]
		public void ValidateCheckDigit_ShouldCorrectlyWeightOddPositionLetters(String value)
		   => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Theory]
		[InlineData("000000000A08")]
		[InlineData("0000000A0008")]
		[InlineData("00000A000008")]
		[InlineData("000A00000008")]
		[InlineData("0A0000000008")]
		public void ValidateCheckDigit_ShouldCorrectlyWeightEvenPositionLetters(String value)
		   => Isin.ValidateCheckDigit(value).Should().BeTrue();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnTrue_WhenInputIsAllZeros()
		   => Isin.ValidateCheckDigit("000000000000").Should().BeTrue();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnTrue_WhenCheckDigitIsCalculatesAsZero()
		   => Isin.ValidateCheckDigit("CA120QWERTY0").Should().BeTrue();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnFalse__WhenInputIsNull()
		   => Isin.ValidateCheckDigit(null!).Should().BeFalse();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnFalse_WhenInputIsEmpty()
		   => Isin.ValidateCheckDigit(String.Empty).Should().BeFalse();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnFalse_WhenInputHasLengthLessThan12()
		   => Isin.ValidateCheckDigit("00000000000").Should().BeFalse();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnFalse_WhenInputHasLengthGreaterThan12()
		   => Isin.ValidateCheckDigit("0000000000000").Should().BeFalse();

		[Fact]
		public void ValidateCheckDigit_ShouldReturnFalse_WhenInputContainsNonAlphanumericCharacter()
		   => Isin.ValidateCheckDigit("US0378#31005").Should().BeFalse();
	}
}
