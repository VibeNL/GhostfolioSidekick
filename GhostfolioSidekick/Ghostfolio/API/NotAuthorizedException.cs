using System.Runtime.Serialization;

namespace GhostfolioSidekick.Ghostfolio.API
{
	[Serializable]
	internal class NotAuthorizedException : Exception
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

		protected NotAuthorizedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}