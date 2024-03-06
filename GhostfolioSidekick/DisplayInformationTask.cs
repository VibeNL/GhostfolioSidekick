using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace GhostfolioSidekick
{
	public class DisplayInformationTask : IScheduledWork
	{
		private readonly ILogger<DisplayInformationTask> logger;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.DisplayInformation;

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

			sb.AppendLine($"CryptoWorkaroundDust : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundDust}");
			sb.AppendLine($"CryptoWorkaroundDustThreshold : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundDustThreshold.ToString(CultureInfo.InvariantCulture)}");
			sb.AppendLine($"CryptoWorkaroundStakeReward : {applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundStakeReward}");
			sb.AppendLine($"DataProviderPreference : {applicationSettings.ConfigurationInstance.Settings.DataProviderPreference}");
			sb.AppendLine($"DeleteUnusedSymbols : {applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols}");

			if (applicationSettings.ConfigurationInstance.Settings.CryptoWorkaroundStakeReward)
			{
				logger.LogWarning("Setting 'CryptoWorkaroundStakeReward' no longer supported");
			}

			logger.LogInformation(sb.ToString());
		}
	}
}