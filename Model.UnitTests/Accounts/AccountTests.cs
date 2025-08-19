using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.UnitTests.Accounts
{
	public class AccountTests
	{
		[Fact]
		public void Account_ShouldSetDefaultSyncValues()
		{
			// Arrange & Act
			var account = new Account("Test Account");

			// Assert
			Assert.Equal("Test Account", account.Name);
			Assert.True(account.SyncActivities);
			Assert.True(account.SyncBalance);
		}

		[Fact]
		public void Account_ShouldAllowSettingSyncValues()
		{
			// Arrange
			var account = new Account("Test Account");

			// Act
			account.SyncActivities = false;
			account.SyncBalance = false;

			// Assert
			Assert.False(account.SyncActivities);
			Assert.False(account.SyncBalance);
		}

		[Fact]
		public void Account_DefaultConstructor_ShouldSetDefaultSyncValues()
		{
			// Arrange & Act
			var account = new Account();

			// Assert
			Assert.True(account.SyncActivities);
			Assert.True(account.SyncBalance);
		}
	}
}