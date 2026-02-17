using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick
{
	public class DisplayInformationTask(
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.DisplayInformation;

		public TimeSpan ExecutionFrequency => TimeSpan.MaxValue;

		public bool ExceptionsAreFatal => true;

		public string Name => "Display Information";

		public Task DoWork(ILogger logger)
		{
			PrintUsedSettings(logger);
			return Task.CompletedTask;
		}

		private void PrintUsedSettings(ILogger logger)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Settings used");
			sb.AppendLine($"GhostfolioUrl : {applicationSettings.GhostfolioUrl}");
			sb.AppendLine($"FileImporterPath : {applicationSettings.FileImporterPath}");
			sb.AppendLine($"DatabasePath : {applicationSettings.DatabasePath}");
			sb.AppendLine($"TrottleTimeout : {applicationSettings.TrottleTimeout}");

			var settings = applicationSettings.ConfigurationInstance.Settings;
			sb.AppendLine($"DataProviderPreference : {settings.DataProviderPreference}");
			sb.AppendLine($"DeleteUnusedSymbols : {settings.DeleteUnusedSymbols}");

			PrintUsedMappings(sb);

			logger.LogInformation("{Message}", sb.ToString());
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