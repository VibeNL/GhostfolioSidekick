namespace GhostfolioSidekick.FileImporter
{
	public class HeaderMapping
	{
		public string SourceName { get; set; }

		public DestinationHeader DestinationHeader { get; set; }

		public bool IsOptional { get; set; } = false;
    }

	public enum DestinationHeader
	{
		Currency,
		Date,
		Quantity,
		Symbol,
		UnitPrice,
		Fee,
		FeeCurrency,
		OrderType,
		Isin,
		Reference,
		Undefined,
		Description,
        CurrencyFeeUK,
        FeeUK,
    }
}