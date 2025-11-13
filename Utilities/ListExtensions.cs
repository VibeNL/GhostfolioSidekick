namespace GhostfolioSidekick.Utilities
{
	public static class ListExtensions
	{
		public static List<T> FilterEmpty<T>(this IEnumerable<T?> list) where T : class
		{
			return [.. list.Where(item => item != null).Select(item => item!)];
		}
	}
}
