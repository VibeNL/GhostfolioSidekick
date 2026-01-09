
namespace GhostfolioSidekick.GhostfolioAPI.API
{
	[Serializable]
	public class NotAuthorizedException(string? message) : Exception(message)
	{
	}
}