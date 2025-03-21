using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericRecord
	{
		public PartialActivityType ActivityType { get; set; }

		public string? Symbol { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		public DateTime Date { get; set; }

		public required string Currency { get; set; }

		private decimal quantity;
		public decimal Quantity
		{
			get => quantity;
			set => quantity = (ActivityType == PartialActivityType.CashWithdrawal || ActivityType == PartialActivityType.CashDeposit) && value == 0 ? 1 : value;
		}

		public decimal UnitPrice { get; set; }

		[Optional]
		public decimal? Fee { get; set; }

		[Optional]
		public decimal? Tax { get; set; }

		[Optional]
		public string? Id { get; set; }
	}
}
