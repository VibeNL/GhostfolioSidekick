using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.GhostfolioAPI
{
	[ExcludeFromCodeCoverage]
	[Serializable]
	public class NotAuthorizedException : Exception
	{
		public NotAuthorizedException()
		{
		}

		public NotAuthorizedException(string? message) : base(message)
		{
		}

		public NotAuthorizedException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}