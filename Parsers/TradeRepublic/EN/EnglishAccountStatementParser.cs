using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{

	public class EnglishAccountStatementParser : BaseSubParser
	{
		private readonly string[] AccountStatementRepayment = ["DATE", "TYPE", "DESCRIPTION", "MONEY IN", "MONEY OUT", "BALANCE"];
		private readonly ColumnAlignment[] column6 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(AccountStatementRepayment, "BALANCE OVERVIEW", column6, true, new EmptyLineHeightLimitMergeStrategy()),
				];
			}
		}

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			yield return PartialActivity.CreateIgnore();
		}
	}
}
