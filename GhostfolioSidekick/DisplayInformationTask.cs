using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace GhostfolioSidekick
{
	public class DisplayInformationTask : IScheduledWork
	{
		private readonly ILogger<DisplayInformationTask> logger;
		private readonly IApplicationSettings applicationSettings;

		public int Priority => int.MinValue;

		public DisplayInformationTask(
			ILogger<DisplayInformationTask> logger,
			IApplicationSettings applicationSettings)
		{
			this.logger = logger;
			this.applicationSettings = applicationSettings;
		}

		public Task DoWork()
		{
			PrintUsedSettings();
			return Task.CompletedTask;
		}

		private void PrintUsedSettings()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Settings used");
			sb.AppendLine($"GhostfolioUrl : {applicationSettings.GhostfolioUrl}");
			sb.AppendLine($"FileImporterPath : {applicationSettings.FileImporterPath}");

			sb.AppendLine($"CryptoWorkaroundStakeReward : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundStakeReward}");
			sb.AppendLine($"CryptoWorkaroundDust : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundDust}");
			sb.AppendLine($"CryptoWorkaroundDustThreshold : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundDustThreshold.ToString(CultureInfo.InvariantCulture)}");

			logger.LogInformation(sb.ToString());
		}
	}
}