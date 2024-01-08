namespace GhostfolioSidekick
{
    internal static class TimestampHelper
    {

        public static long DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            DateTimeOffset dto = new(dateTime.ToUniversalTime());
            return dto.ToUnixTimeMilliseconds();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}