using AutoFixture;
using Shouldly;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class TransactionReferenceUtilitiesTests
	{
		[Fact]
		public void GetComment_WithTransactionIdAndSymbolProfile_ShouldReturnFormattedComment()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = "12345"
			};
			var symbolProfile = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.Symbol, "AAPL")
				.Create();

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity, symbolProfile);

			// Assert
			result.ShouldBe("Transaction Reference: [12345] (Details: asset AAPL)");
		}

		[Fact]
		public void GetComment_WithTransactionIdAndNullSymbolProfile_ShouldReturnFormattedCommentWithEmptySymbol()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = "12345"
			};

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity, null);

			// Assert
			result.ShouldBe("Transaction Reference: [12345] (Details: asset <EMPTY>)");
		}

		[Fact]
		public void GetComment_WithNullTransactionIdAndSymbolProfile_ShouldReturnEmptyString()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = null!
			};
			var symbolProfile = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.Symbol, "AAPL")
				.Create();

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity, symbolProfile);

			// Assert
			result.ShouldBeEmpty();
		}

		[Fact]
		public void GetComment_WithEmptyTransactionIdAndSymbolProfile_ShouldReturnEmptyString()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = string.Empty
			};
			var symbolProfile = new Fixture()
				.Build<SymbolProfile>()
				.With(x => x.Symbol, "AAPL")
				.Create();

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity, symbolProfile);

			// Assert
			result.ShouldBeEmpty();
		}

		[Fact]
		public void GetComment_WithTransactionId_ShouldReturnFormattedComment()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = "12345"
			};

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity);

			// Assert
			result.ShouldBe("Transaction Reference: [12345]");
		}

		[Fact]
		public void GetComment_WithNullTransactionId_ShouldReturnEmptyString()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = null!
			};

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity);

			// Assert
			result.ShouldBeEmpty();
		}

		[Fact]
		public void GetComment_WithEmptyTransactionId_ShouldReturnEmptyString()
		{
			// Arrange
			var activity = new BuySellActivity
			{
				TransactionId = string.Empty
			};

			// Act
			var result = TransactionReferenceUtilities.GetComment(activity);

			// Assert
			result.ShouldBeEmpty();
		}
	}
}
