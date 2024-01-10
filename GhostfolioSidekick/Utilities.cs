namespace GhostfolioSidekick
{
	internal class Utilities
	{
		internal static T ParseEnum<T>(string value) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			return Enum.Parse<T>(value);
		}
	}
}
