namespace GhostfolioSidekick.GhostfolioAPI
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

		internal static T? ParseOptionalEnum<T>(string? value) where T : struct
		{
			if (string.IsNullOrEmpty(value))
			{
				return default;
			}

			return Enum.Parse<T>(value);
		}
	}
}
