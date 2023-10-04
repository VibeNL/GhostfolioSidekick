﻿using GhostfolioSidekick.Model;

namespace GhostfolioSidekick
{
	internal static class CurrencyHelper
	{
		public static Currency ParseCurrency(string currency)
		{
			return (Currency)Enum.Parse(typeof(Currency), currency);
		}
	}
}