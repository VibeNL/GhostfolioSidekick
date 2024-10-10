using System.Collections.Generic;
using System;

namespace YahooFinanceApi
{
	public class SearchResult
	{
		public IReadOnlyDictionary<string, dynamic> Fields { get; private set; }

		// ctor
		internal SearchResult(IReadOnlyDictionary<string, dynamic> fields) => Fields = fields;

		public dynamic this[string fieldName] => Fields[fieldName];
		public dynamic this[Field field] => Fields[field.ToString()];

		public string Symbol => this["Symbol"];
		public string LongName => this["Longname"];
		public double Score => this["Score"];
		public string QuoteType => this["QuoteType"];


	}
}