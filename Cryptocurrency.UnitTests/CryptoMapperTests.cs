namespace GhostfolioSidekick.Cryptocurrency.UnitTests
{
	public class CryptoMapperTests
	{
		[Fact]
		public void GetFullname_ExistingSymbol_ReturnsFullname()
		{
			// Arrange
			string symbol = "BTC";
			string expectedFullname = "Bitcoin";

			// Act
			string result = CryptoMapper.Instance.GetFullname(symbol);

			// Assert
			Assert.Equal(expectedFullname, result, true);
		}

		[Fact]
		public void GetFullname_NonexistentSymbol_ReturnsOriginalSymbol()
		{
			// Arrange
			string symbol = "IDONOTEXISTS";

			// Act
			string result = CryptoMapper.Instance.GetFullname(symbol);

			// Assert
			Assert.Equal(symbol, result);
		}

		[Fact]
		public void GetFullname_NullSymbol_ReturnsNull()
		{
			// Arrange
			string? symbol = null;

			// Act
			string result = CryptoMapper.Instance.GetFullname(symbol!);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetFullname_EmptySymbol_ReturnsEmptyString()
		{
			// Arrange
			string symbol = string.Empty;

			// Act
			string result = CryptoMapper.Instance.GetFullname(symbol);

			// Assert
			Assert.Equal(string.Empty, result);
		}
	}
}
