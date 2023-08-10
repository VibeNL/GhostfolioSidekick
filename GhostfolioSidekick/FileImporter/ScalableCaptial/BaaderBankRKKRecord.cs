using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{
    public class BaaderBankRKKRecord
    {
        [Name("XXX-UMART")]
        public string OrderType { get; set; }

        [Name("XXX-TEXT1")]
        public string Symbol { get; set; }

        [Name("XXX-TEXT2")]
        public string Isin { get; set; }

        [Name("XXX-SALDO")]
        public decimal? UnitPrice { get; set; }

        [Name("XXX-WHG")]
        public string Currency { get; set; }

        [Name("XXX-REFNR1")]
        public string Reference { get; set; }

        [Format("yyyyMMdd")]
        [Name("XXX-VALUTA")]
        public string Date { get; set; }

        [Name("XXX-TEXT3")]
        public string Quantity { get; set; }
    }
}
