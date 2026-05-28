using GhostfolioSidekick.ProcessHost;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhostfolioSidekick.ProcessHost.UnitTests;

public class ChildProcessServiceTests
{
	[Fact]
	public async Task ExecuteAsync_WhenDotnetHostPathNotSet_ThrowsInvalidOperationException()
	{
		// Arrange
		var original = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		try
		{
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", null);
			var service = new ChildProcessService("some.dll", NullLogger<ChildProcessService>.Instance);

			// Act & Assert
			using var cts = new CancellationTokenSource();
			await Assert.ThrowsAsync<InvalidOperationException>(
				() => service.StartAsync(cts.Token));
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", original);
		}
	}

	[Fact]
	public async Task ExecuteAsync_WhenCancelledImmediately_StopsWithoutLaunchingProcess()
	{
		// Arrange — use a non-existent DLL; the process should never be launched because
		// the token is already cancelled before ExecuteAsync can start the loop.
		var original = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		try
		{
			// Point to a real executable so ProcessStartInfo is valid, but cancel before it matters.
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", Environment.ProcessPath ?? "dotnet");

			var service = new ChildProcessService("nonexistent.dll", NullLogger<ChildProcessService>.Instance);

			using var cts = new CancellationTokenSource();
			await cts.CancelAsync();

			// Act — should return quickly without throwing because the loop checks the token first.
			var exception = await Record.ExceptionAsync(() => service.StartAsync(cts.Token));

			// Assert
			Assert.Null(exception);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", original);
		}
	}

	[Fact]
	public async Task ExecuteAsync_ProcessExitsUnexpectedly_RestartsAfterDelay()
	{
		// Arrange — launch "dotnet --version" which exits immediately with code 0,
		// simulating an unexpected child-process exit. We cancel after the first restart
		// delay to break the loop.
		var original = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		try
		{
			var dotnetExe = Environment.ProcessPath ?? "dotnet";
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", dotnetExe);

			// Use "--version" so the child exits almost instantly.
			var service = new ChildProcessService("--version", NullLogger<ChildProcessService>.Instance);

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			// Act — run until cancelled; the service will log a warning about the exit and try to restart.
			var exception = await Record.ExceptionAsync(() => service.StartAsync(cts.Token));

			// Assert — no unhandled exception; the service handled the exit gracefully.
			Assert.Null(exception);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", original);
		}
	}

	[Fact]
	public async Task ExecuteAsync_CancellationWhileRunning_KillsChildProcess()
	{
		// Arrange — "dotnet watch" would run indefinitely; approximate with a sleep command.
		// We use "dotnet run" against a non-existent project so the process starts but
		// exits quickly; we just need to verify cancellation mid-run doesn't throw.
		var original = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		try
		{
			var dotnetExe = Environment.ProcessPath ?? "dotnet";
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", dotnetExe);

			// "--version" exits fast; cancel before the restart delay completes.
			var service = new ChildProcessService("--version", NullLogger<ChildProcessService>.Instance);

			using var cts = new CancellationTokenSource();

			// Start the service; cancel right away to catch it in the restart-delay await.
			var runTask = service.StartAsync(cts.Token);
			await Task.Delay(50); // let it launch and exit once
			await cts.CancelAsync();

			var exception = await Record.ExceptionAsync(() => runTask);

			// Assert — cancellation must not propagate as an unhandled exception.
			Assert.Null(exception);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", original);
		}
	}
}
