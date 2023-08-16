using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick
{
	public class ConfigurationSettings : IConfigurationSettings
	{
		public string? FileImporterPath => Environment.GetEnvironmentVariable("FileImporterPath");
	}
}
