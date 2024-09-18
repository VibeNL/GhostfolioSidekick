namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO
{
	internal static class UnitTimeStamp
	{

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
			return dateTime;
		}
	}
}