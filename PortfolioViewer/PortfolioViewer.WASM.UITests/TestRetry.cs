namespace PortfolioViewer.WASM.UITests
{
	/// <summary>
	/// Retry helper for flaky tests (replaces the xRetry.v3 <c>RetryFact</c> attribute).
	/// Runs the test body up to <paramref name="maxAttempts"/> times until it passes;
	/// the exception from the final failed attempt is rethrown.
	/// </summary>
	public static class TestRetry
	{
		public const int DefaultMaxAttempts = 3;

		public static void Run(Action test, int maxAttempts = DefaultMaxAttempts)
		{
			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					test();
					return;
				}
				catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
				{
					Console.Error.WriteLine($"[TestRetry] Attempt {attempt} failed ({ex.GetType().Name}: {ex.Message}), retrying...");
				}
			}
		}

		public static async Task RunAsync(Func<Task> test, int maxAttempts = DefaultMaxAttempts)
		{
			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					await test();
					return;
				}
				catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
				{
					Console.Error.WriteLine($"[TestRetry] Attempt {attempt} failed ({ex.GetType().Name}: {ex.Message}), retrying...");
				}
			}
		}
	}
}
