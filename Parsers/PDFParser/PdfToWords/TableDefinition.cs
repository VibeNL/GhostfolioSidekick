namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record TableDefinition(
		string[] Headers, 
		string StopWord, 
		ColumnAlignment[] ColumnAlignments, 
		bool IsRequired = false,
		IMergeRowStrategy? MergeStrategy = null);
}
