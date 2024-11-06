using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.GhostfolioAPI
{
	[ExcludeFromCodeCoverage]
	[Serializable]
	public class NotAuthorizedException : Exception
	{
		public NotAuthorizedException(string? message) : base(message)
		{
		}
	}
}