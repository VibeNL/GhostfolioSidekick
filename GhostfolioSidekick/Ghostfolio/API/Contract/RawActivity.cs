﻿namespace GhostfolioSidekick.Ghostfolio.API.Contract
{
	public class RawActivity
	{
		public string AccountId { get; set; }

		public SymbolProfile SymbolProfile { get; set; }

		public string Comment { get; set; }

		public DateTime Date { get; set; }

		public decimal Fee { get; set; }

		public decimal Quantity { get; set; }

		public ActivityType Type { get; set; }

		public decimal UnitPrice { get; set; }

		public Guid Id { get; set; }
	}
}