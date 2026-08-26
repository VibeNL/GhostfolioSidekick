namespace GhostfolioSidekick.Tools.TestUtilities
{
	/// <summary>
	/// Retry helper for flaky tests (replaces the xRetry.v3 <c>RetryFact</c> attribute).
	/// Runs the test body up to <paramref name="maxAttempts"/> times until it passes.
	/// On success, returns <c>true</c> so callers can assert on the result (keeps the test
	/// method assertion-bearing, e.g. for Sonar S2699). If every attempt fails, the exception
	/// from the final attempt is rethrown; <c>false</c> is only returned when
	/// <paramref name="maxAttempts"/> is less than 1.
	/// </summary>
	public static class TestRetry
	{
		public const int DefaultMaxAttempts = 3;

		public static void Run(Action test, int maxAttempts = DefaultMaxAttempts)
		{
			RunAsync(() =>
			{
				test();
				return Task.CompletedTask;
			}, maxAttempts).GetAwaiter().GetResult();
		}

		public static async Task<bool> RunAsync(Func<Task> test, int maxAttempts = DefaultMaxAttempts)
		{
			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					await test();
					return true;
				}
				catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
				{
					await Console.Error.WriteLineAsync($"[TestRetry] Attempt {attempt} failed ({ex.GetType().Name}: {ex.Message}), retrying...");
				}
			}

			// Only reachable when maxAttempts < 1; otherwise the final attempt's exception rethrows.
			return false;
		}
	}
}
