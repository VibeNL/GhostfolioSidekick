//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.Model.Activities.Types;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
//{
//	public class TransactionReferenceUtilitiesTests
//	{
//		[Fact]
//		public void GetComment_WithSymbolProfile_ShouldReturnExpectedString()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, "123").Create();
//			var symbolProfile = new Fixture().Build<SymbolProfile>().With(x => x.Symbol, "ABC").Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity, symbolProfile);

//			// Assert
//			result.Should().Be("Transaction Reference: [123] (Details: asset ABC)");
//		}

//		[Fact]
//		public void GetComment_WithEmptySymbolProfile_ShouldReturnExpectedString()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, "123").Create();
//			var symbolProfile = new Fixture().Build<SymbolProfile>().With(x => x.Symbol, (string)null!).Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity, symbolProfile);

//			// Assert
//			result.Should().Be("Transaction Reference: [123] (Details: asset <EMPTY>)");
//		}

//		[Fact]
//		public void GetComment_WithNullSymbolProfile_ShouldReturnExpectedString()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, "123").Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity, null);

//			// Assert
//			result.Should().Be("Transaction Reference: [123] (Details: asset <EMPTY>)");
//		}

//		[Fact]
//		public void GetComment_SymbolProfile_WithEmptyTransactionId_ShouldReturnEmpty()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, string.Empty).Create();
//			var symbolProfile = new Fixture().Build<SymbolProfile>().With(x => x.Symbol, "ABC").Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity, null);

//			// Assert
//			result.Should().BeEmpty();
//		}

//		[Fact]
//		public void GetComment_WithoutSymbolProfile_ShouldReturnExpectedString()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, "123").Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity);

//			// Assert
//			result.Should().Be("Transaction Reference: [123]");
//		}

//		[Fact]
//		public void GetComment_EmptyTransactionWithoutSymbolProfile_ShouldReturnExpectedString()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, string.Empty).Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity);

//			// Assert
//			result.Should().BeEmpty();
//		}

//		[Fact]
//		public void GetComment_WithEmptyTransactionId_ShouldThrowNotSupportedException()
//		{
//			// Arrange
//			var activity = new Fixture().Build<BuySellActivity>().With(x => x.TransactionId, string.Empty).Create();

//			// Act
//			var result = TransactionReferenceUtilities.GetComment(activity, null);

//			// Assert
//			result.Should().BeEmpty();
//		}

//		[Fact]
//		public void ParseComment_WithValidComment_ShouldReturnTransactionId()
//		{
//			// Arrange
//			var activity = new Contract.Activity
//			{
//				Comment = "Transaction Reference: [123]",
//				SymbolProfile = Contract.SymbolProfile.Empty(Model.Currency.EUR, string.Empty)
//			};

//			// Act
//			var result = TransactionReferenceUtilities.ParseComment(activity);

//			// Assert
//			result.Should().Be("123");
//		}

//		[Fact]
//		public void ParseComment_WithInvalidComment_ShouldReturnEmptyString()
//		{
//			// Arrange
//			var activity = new Contract.Activity
//			{
//				Comment = "Invalid Comment",
//				SymbolProfile = Contract.SymbolProfile.Empty(Model.Currency.EUR, string.Empty)
//			};

//			// Act
//			var result = TransactionReferenceUtilities.ParseComment(activity);

//			// Assert
//			result.Should().Be(string.Empty);
//		}

//		[Fact]
//		public void ParseComment_WithEmptyComment_ShouldReturnNull()
//		{
//			// Arrange
//			var activity = new Contract.Activity
//			{
//				Comment = string.Empty,
//				SymbolProfile = Contract.SymbolProfile.Empty(Model.Currency.EUR, string.Empty)
//			};

//			// Act
//			var result = TransactionReferenceUtilities.ParseComment(activity);

//			// Assert
//			result.Should().BeNull();
//		}
//	}
//}
