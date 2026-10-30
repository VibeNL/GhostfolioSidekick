using CsvHelper;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
    [CsvRecord]
    public partial class TradeRepublicCsvRecord
    {
        [CsvColumn("datetime")]
        public DateTime DateTime { get; set; }

        [CsvColumn("date")]
        public DateOnly Date { get; set; }

        [CsvColumn("account_type")]
        public string? AccountType { get; set; }

        [CsvColumn("category")]
        public required string Category { get; set; }

        [CsvColumn("type")]
        public required string Type { get; set; }

        [CsvColumn("asset_class")]
        public string? AssetClass { get; set; }

        [CsvColumn("name")]
        public string? Name { get; set; }

        [CsvColumn("symbol")]
        public string? Symbol { get; set; }

        [CsvColumn("shares")]
        public decimal? Shares { get; set; }

        [CsvColumn("price")]
        public decimal? Price { get; set; }

        [CsvColumn("amount")]
        public decimal? Amount { get; set; }

        [CsvColumn("fee")]
        public decimal? Fee { get; set; }

        [CsvColumn("tax")]
        public decimal? Tax { get; set; }

        [CsvColumn("currency")]
        public required string Currency { get; set; }

        [CsvColumn("original_amount")]
        public decimal? OriginalAmount { get; set; }

        [CsvColumn("original_currency")]
        public string? OriginalCurrency { get; set; }

        [CsvColumn("fx_rate")]
        public decimal? FxRate { get; set; }

        [CsvColumn("description")]
        public string? Description { get; set; }

        [CsvColumn("transaction_id")]
        public required string TransactionId { get; set; }

        [CsvColumn("counterparty_name")]
        public string? CounterpartyName { get; set; }

        [CsvColumn("counterparty_iban")]
        public string? CounterpartyIban { get; set; }

        [CsvColumn("payment_reference")]
        public string? PaymentReference { get; set; }

        [CsvColumn("mcc_code")]
        public string? MccCode { get; set; }
    }
}
