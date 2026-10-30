namespace GhostfolioSidekick.Parsers
{
	/// <summary>
	/// Thrown by a parser when a row contains a transaction type it does not support.
	/// Depending on the 'parsers.skip.unknown.transaction.types' setting the row is either skipped or the import is aborted.
	/// </summary>
	public class UnknownTransactionTypeException : NotSupportedException
	{
		public UnknownTransactionTypeException()
		{
			ParserName = "Unknown";
		}

		public UnknownTransactionTypeException(string? message)
			: base(message)
		{
			ParserName = "Unknown";
		}

		public UnknownTransactionTypeException(string? message, Exception? innerException)
			: base(message, innerException)
		{
			ParserName = "Unknown";
		}

		public UnknownTransactionTypeException(string parserName, string? transactionType)
			: base($"{parserName} transaction type '{transactionType}' is not supported.")
		{
			ParserName = parserName;
			TransactionType = transactionType;
		}

		public string ParserName { get; }

		public string? TransactionType { get; }
	}
}
