namespace GhostfolioSidekick.Parsers
{
	[Serializable]
	internal class NoImporterAvailableException : Exception
	{
		public NoImporterAvailableException()
		{
		}

		public NoImporterAvailableException(string? message) : base(message)
		{
		}

		public NoImporterAvailableException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}