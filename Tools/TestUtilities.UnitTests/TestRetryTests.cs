namespace GhostfolioSidekick.Tools.TestUtilities.UnitTests
{
	public class TestRetryTests
	{
		private class FlakyException : Exception
		{
			public FlakyException(string message) : base(message)
			{
			}
		}

		private static void ThrowFlaky(string message) => throw new FlakyException(message);

		private static void ThrowCanceled() => throw new OperationCanceledException();

		[Fact]
		public async Task RunAsync_PassesOnFirstAttempt_ReturnsTrueAfterSingleCall()
		{
			var calls = 0;

			var result = await TestRetry.RunAsync(() =>
			{
				calls++;
				return Task.CompletedTask;
			});

			Assert.True(result);
			Assert.Equal(1, calls);
		}

		[Fact]
		public async Task RunAsync_FailsThenSucceeds_RetriesUntilPass()
		{
			var calls = 0;

			var result = await TestRetry.RunAsync(() =>
			{
				calls++;
				if (calls < 3)
				{
					ThrowFlaky($"failure {calls}");
				}

				return Task.CompletedTask;
			});

			Assert.True(result);
			Assert.Equal(3, calls);
		}

		[Fact]
		public async Task RunAsync_AllAttemptsFail_RethrowsFinalException()
		{
			var calls = 0;

			var ex = await Assert.ThrowsAsync<FlakyException>(() => TestRetry.RunAsync(() =>
			{
				calls++;
				ThrowFlaky($"failure {calls}");
				return Task.CompletedTask;
			}, maxAttempts: 3));

			Assert.Equal(3, calls);
			Assert.Equal("failure 3", ex.Message);
		}

		[Fact]
		public async Task RunAsync_OperationCanceledException_DoesNotRetry()
		{
			var calls = 0;

			await Assert.ThrowsAsync<OperationCanceledException>(() => TestRetry.RunAsync(() =>
			{
				calls++;
				ThrowCanceled();
				return Task.CompletedTask;
			}));

			Assert.Equal(1, calls);
		}

		[Fact]
		public async Task RunAsync_SingleAttempt_Fails_RethrowsWithoutRetry()
		{
			var calls = 0;

			await Assert.ThrowsAsync<FlakyException>(() => TestRetry.RunAsync(() =>
			{
				calls++;
				ThrowFlaky("failure 1");
				return Task.CompletedTask;
			}, maxAttempts: 1));

			Assert.Equal(1, calls);
		}

		[Fact]
		public async Task RunAsync_ZeroAttempts_ReturnsFalseWithoutCallingTest()
		{
			var calls = 0;

			var result = await TestRetry.RunAsync(() =>
			{
				calls++;
				return Task.CompletedTask;
			}, maxAttempts: 0);

			Assert.False(result);
			Assert.Equal(0, calls);
		}

		[Fact]
		public void Run_Sync_PassesAfterRetry_Completes()
		{
			var calls = 0;

			TestRetry.Run(() =>
			{
				calls++;
				if (calls < 2)
				{
					ThrowFlaky($"failure {calls}");
				}
			});

			Assert.Equal(2, calls);
		}

		[Fact]
		public void Run_Sync_AllAttemptsFail_RethrowsFinalException()
		{
			var calls = 0;

			var ex = Assert.Throws<FlakyException>(() => TestRetry.Run(() =>
			{
				calls++;
				ThrowFlaky($"failure {calls}");
			}, maxAttempts: 2));

			Assert.Equal(2, calls);
			Assert.Equal("failure 2", ex.Message);
		}
	}
}
