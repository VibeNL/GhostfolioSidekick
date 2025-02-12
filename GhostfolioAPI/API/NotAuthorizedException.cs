
namespace GhostfolioSidekick.GhostfolioAPI.API
{
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "No Need")]
	public class NotAuthorizedException : Exception
	{
		public NotAuthorizedException(string? message) : base(message)
		{
		}
	}
}