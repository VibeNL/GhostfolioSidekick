namespace GhostfolioSidekick
{
	internal static class DateTimeExtensions
	{
		internal static string ToInvariantDateOnlyString(this DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd");
		}

		internal static string ToInvariantString(this DateOnly dateOnly)
		{
			return dateOnly.ToString("yyyy-MM-dd");
		}

		internal static string ToInvariantString(this TimeOnly timeOnly)
		{
			return timeOnly.ToString("HH:mm");
		}
	}
}
