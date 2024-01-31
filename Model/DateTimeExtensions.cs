namespace GhostfolioSidekick.Model
{
	public static class DateTimeExtensions
	{
		public static string ToInvariantString(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss:zzz");
		}

		public static string ToInvariantDateOnlyString(this DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd");
		}
	}
}
