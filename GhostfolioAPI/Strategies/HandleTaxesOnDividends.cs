using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class HandleTaxesOnDividends(Settings settings, IExchangeRateService exchangeRateService) : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.TaxesOnDividends;

		public async Task Execute(Holding holding)
		{
			if (!settings.SubstractTaxesOnDividendFromDividend)
			{
				return;
			}

			var activities = holding.Activities.OfType<DividendActivity>().ToList();

			foreach (var activity in activities)
			{
				foreach (var tax in activity.Taxes)
				{
					var exchangeRate = await exchangeRateService.GetConversionRate(tax.Currency, activity.Amount.Currency, activity.Date);
					var moneyInNativeCurrency = tax.Amount * exchangeRate;
					activity.Amount.Amount -= moneyInNativeCurrency;
				}

				activity.Taxes = [];
			}
		}
	}
}
