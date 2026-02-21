using System.Reflection;

namespace GhostfolioSidekick.PortfolioViewer.Common
{
	public static class VersionInfo
	{
		private static string? _version;

		public static string Version
		{
			get
			{
				if (_version == null)
				{
					var assembly = Assembly.GetExecutingAssembly();
					var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
					_version = informationalVersion?.InformationalVersion ?? "unknown";
				}
				return _version;
			}
		}
	}
}
