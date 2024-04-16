using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick
{
	public class DisplayInformationTask : IScheduledWork
	{
		private readonly ILogger<DisplayInformationTask> logger;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.DisplayInformation;

		public TimeSpan ExecutionFrequency => TimeSpan.MaxValue;

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

			Settings settings = applicationSettings.ConfigurationInstance.Settings;
			sb.AppendLine($"DustThreshold : {settings.DustThreshold}");
			sb.AppendLine($"CryptoWorkaroundDustThreshold : {settings.CryptoWorkaroundDustThreshold}");
			sb.AppendLine($"CryptoWorkaroundStakeReward : {settings.CryptoWorkaroundStakeReward}");
			sb.AppendLine($"DataProviderPreference : {settings.DataProviderPreference}");
			sb.AppendLine($"DeleteUnusedSymbols : {settings.DeleteUnusedSymbols}");

			PrintUsedMappings(sb);

			logger.LogInformation(sb.ToString());

			if (settings.CryptoWorkaroundStakeRewardObsolete)
			{
				logger.LogWarning("Setting 'use.crypto.workaround.stakereward.as.dividends' is obsolete and is no longer in use");
			}

			if (settings.CryptoWorkaroundDustObsolete)
			{
				logger.LogWarning("Setting 'use.crypto.workaround.dust' is obsolete and is no longer in use");
			}
		}

		private void PrintUsedMappings(StringBuilder sb)
		{
			var mappings = applicationSettings.ConfigurationInstance.Mappings ?? [];
			sb.AppendLine($"Defined mappings: #{mappings.Length}");
			foreach (var mapping in mappings)
			{
				sb.AppendLine($"Mapping {mapping.MappingType}: {mapping.Source} -> {mapping.Target}");
			}
		}
	}
}