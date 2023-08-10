using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
    [Delimiter(",")]
    public class DeGiroRecord
    {
        [Format("dd-MM-yyyy")]
        public DateOnly Datum { get; set; }

        public TimeOnly Tijd { get; set; }

        public DateOnly Valutadatum { get; set; }

        public string Product { get; set; }

        public string ISIN { get; set; }

        public string Omschrijving { get; set; }

        public string FX { get; set; }

        public string Mutatie { get; set; }

        [Index(8)]
        [CultureInfo("nl-NL")]
        public decimal? Total { get; set; }

        public string Saldo { get;set; }

        [Name("Order Id")]
        public string OrderId { get; set; }


    }
}
