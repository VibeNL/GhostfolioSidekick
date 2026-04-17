using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
    public class TradeRepublicCsvRecord
    {
        [Name("datetime")]
        public DateTime DateTime { get; set; }

        [Name("date")]
        public DateOnly Date { get; set; }

        [Name("account_type")]
        public string? AccountType { get; set; }

        [Name("category")]
        public required string Category { get; set; }

        [Name("type")]
        public required string Type { get; set; }

        [Name("asset_class")]
        public string? AssetClass { get; set; }

        [Name("name")]
        public string? Name { get; set; }

        [Name("symbol")]
        public string? Symbol { get; set; }

        [Name("shares")]
        public decimal? Shares { get; set; }

        [Name("price")]
        public decimal? Price { get; set; }

        [Name("amount")]
        public decimal? Amount { get; set; }

        [Name("fee")]
        public decimal? Fee { get; set; }

        [Name("tax")]
        public decimal? Tax { get; set; }

        [Name("currency")]
        public required string Currency { get; set; }

        [Name("original_amount")]
        public decimal? OriginalAmount { get; set; }

        [Name("original_currency")]
        public string? OriginalCurrency { get; set; }

        [Name("fx_rate")]
        public decimal? FxRate { get; set; }

        [Name("description")]
        public string? Description { get; set; }

        [Name("transaction_id")]
        public required string TransactionId { get; set; }

        [Name("counterparty_name")]
        public string? CounterpartyName { get; set; }

        [Name("counterparty_iban")]
        public string? CounterpartyIban { get; set; }

        [Name("payment_reference")]
        public string? PaymentReference { get; set; }

        [Name("mcc_code")]
        public string? MccCode { get; set; }
    }
}
