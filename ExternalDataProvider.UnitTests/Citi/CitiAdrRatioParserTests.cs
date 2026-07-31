using GhostfolioSidekick.ExternalDataProvider.Citi;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Citi
{
	public class CitiAdrRatioParserTests
	{
		[Fact]
		public void TryParseSharesPerReceipt_SamsungGdrExample_Returns25()
		{
			// Arrange
			var content = "<td>Ratio (ORD:DRS)&nbsp;</td><td class=\"mwRight\">25      :1       </td>";

			// Act
			var result = CitiAdrRatioParser.TryParseSharesPerReceipt(content);

			// Assert
			Assert.Equal(25m, result);
		}

		[Theory]
		[InlineData("Ratio (ORD:DRS) 4:1", 4)]
		[InlineData("Ratio(ORD:DRS)10:1", 10)]
		[InlineData("Ratio (ORD : DRS) 0.5:1", 0.5)]
		public void TryParseSharesPerReceipt_VariousFormats_ParsesCorrectly(string content, decimal expected)
		{
			// Act
			var result = CitiAdrRatioParser.TryParseSharesPerReceipt(content);

			// Assert
			Assert.Equal(expected, result);
		}

		[Fact]
		public void TryParseSharesPerReceipt_NonUnityDrsSide_DividesCorrectly()
		{
			// Arrange
			var content = "Ratio (ORD:DRS) 1:2";

			// Act
			var result = CitiAdrRatioParser.TryParseSharesPerReceipt(content);

			// Assert
			Assert.Equal(0.5m, result);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("No ratio information here")]
		public void TryParseSharesPerReceipt_NoMatch_ReturnsNull(string? content)
		{
			// Act
			var result = CitiAdrRatioParser.TryParseSharesPerReceipt(content);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void TryGetCusipFromIsin_UsIsin_ReturnsCusip()
		{
			// Act
			var result = CitiAdrRatioParser.TryGetCusipFromIsin("US7960508882");

			// Assert
			Assert.Equal("796050888", result);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("KR7005930003")]
		[InlineData("US123")]
		public void TryGetCusipFromIsin_InvalidOrNonUs_ReturnsNull(string? isin)
		{
			// Act
			var result = CitiAdrRatioParser.TryGetCusipFromIsin(isin);

			// Assert
			Assert.Null(result);
		}
	}
}
