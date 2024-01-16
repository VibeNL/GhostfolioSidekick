namespace GhostfolioSidekick.Model
{
	public class Platform(
		string name,
		string? id, 
		string? url)
	{
		public string Name { get; set; } = name;

		public string? Url { get; set; } = url;

		public string? Id { get; set; } = id;
	}
}