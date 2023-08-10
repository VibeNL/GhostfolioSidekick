using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{
    public class BaaderBankWUMRecord
    {
        [Format("yyyyMMdd")]
        [Name("XXX-BUDAT")]
        public DateOnly Date { get; set; }

        [Name("XXX-WPKURS")]
        public decimal? UnitPrice { get; set; }

        [Name("XXX-WHGAB")]
        public string Currency { get; set; }

        [Name("XXX-NW")]
        public decimal? Quantity { get; set; }

        [Name("XXX-WPNR")]
        public string Isin { get; set; }

        [Name("XXX-WPGART")]
        public string OrderType { get; set; }

        [Name("XXX-EXTORDID")]
        public string Reference { get; set; }

        [Name("XXX-BELEGU")]
        public string SavingsPlanIdentification { get; set; }
    }
}
