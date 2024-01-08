﻿using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public abstract class DeGiroRecordBase
	{
		public virtual DateOnly Date { get; set; }

		public virtual TimeOnly Time { get; set; }

		public virtual DateOnly CurrencyDate { get; set; }

		public virtual string Product { get; set; }

		public virtual string ISIN { get; set; }

		public virtual string Description { get; set; }

		public virtual string FX { get; set; }

		public virtual string Mutation { get; set; }
		
		public virtual decimal? Total { get; set; }

		public virtual string BalanceCurrency { get; set; }

		public virtual decimal Balance { get; set; }

		public virtual string TransactionId { get; set; }

		public abstract ActivityType? GetActivityType();

		public abstract bool IsFee();

		public abstract bool IsTaxes();

		public abstract decimal GetQuantity();

		public abstract decimal GetUnitPrice();
	}
}
