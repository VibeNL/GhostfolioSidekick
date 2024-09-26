using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace YahooFinanceApi
{
    internal static class Helper
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ToUtcFrom(this DateTime dt, TimeZoneInfo tzi)
        {
            switch (dt.Kind)
            {
                case DateTimeKind.Local:
                    return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), tzi);
                case DateTimeKind.Unspecified:
                    return TimeZoneInfo.ConvertTimeToUtc(dt, tzi);
                case DateTimeKind.Utc:
                    return dt;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static string ToUnixTimestamp(this DateTime dt) =>
            DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                .Subtract(Epoch)
                .TotalSeconds
                .ToString("F0");

        internal static string Name<T>(this T @enum)
        {
            string name = @enum.ToString();
            if (typeof(T).GetMember(name).First().GetCustomAttribute(typeof(EnumMemberAttribute)) is EnumMemberAttribute attr && attr.IsValueSetExplicitly)
                name = attr.Value;
            return name;
        }

        internal static string ToLowerCamel(this string pascal) =>
            pascal.Substring(0, 1).ToLower() + pascal.Substring(1);

        internal static string ToPascal(this string lowerCamel) =>
            lowerCamel.Substring(0, 1).ToUpper() + lowerCamel.Substring(1);

        internal static IEnumerable<string> Duplicates(this IEnumerable<string> items)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return items.Where(item => !hashSet.Add(item));
        }

    }
}
