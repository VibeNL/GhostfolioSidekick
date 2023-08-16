using System.Runtime.Serialization;

namespace GhostfolioSidekick.FileImporter
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

        protected NoImporterAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}