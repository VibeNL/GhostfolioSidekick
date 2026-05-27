using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ProcessHost;

/// <summary>
/// Hosts a child .NET process (identified by its DLL path) as a BackgroundService.
/// Automatically restarts the child process if it exits unexpectedly.
/// </summary>
internal sealed class ChildProcessService : BackgroundService
{
	private readonly string _dllPath;
	private readonly ILogger<ChildProcessService> _logger;
	private readonly TimeSpan _restartDelay = TimeSpan.FromSeconds(5);

	public ChildProcessService(string dllPath, ILogger<ChildProcessService> logger)
	{
		_dllPath = dllPath;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogInformation("Starting child process: dotnet {DllPath}", _dllPath);

			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = _dllPath,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				},
				EnableRaisingEvents = true,
			};

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data is not null)
				{
					_logger.LogInformation("[{DllPath}] {Line}", _dllPath, e.Data);
				}
			};

			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data is not null)
				{
					_logger.LogError("[{DllPath}] {Line}", _dllPath, e.Data);
				}
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			try
			{
				await process.WaitForExitAsync(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				// Host is shutting down — kill the child gracefully
				if (!process.HasExited)
				{
					_logger.LogInformation("Stopping child process: {DllPath}", _dllPath);
					process.Kill(entireProcessTree: true);
				}

				return;
			}

			_logger.LogWarning(
				"Child process {DllPath} exited with code {ExitCode}. Restarting in {Delay}s…",
				_dllPath,
				process.ExitCode,
				_restartDelay.TotalSeconds);

			await Task.Delay(_restartDelay, stoppingToken);
		}
	}
}
