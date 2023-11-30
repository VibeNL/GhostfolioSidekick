namespace GhostfolioSidekick.Model
{
	public class Platform
	{
		public Platform(string? id, string name, string? url)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
			}

			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentException($"'{nameof(url)}' cannot be null or empty.", nameof(url));
			}

			Id = id;
			Name = name;
			Url = url;
		}

		public string Name { get; set; }

		public string? Url { get; set; }

		public string? Id { get; set; }
	}
}