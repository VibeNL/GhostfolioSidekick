using GhostfolioSidekick.Database;
using PortfolioViewer.Model;

namespace PortfolioViewer.ApiService
{
	public class PortfolioManager
	{
		internal static Portfolio LoadPorfolio(DatabaseContext databaseContext)
		{
			return new Portfolio
			{ 
				Accounts = databaseContext.Accounts.Select(Map).ToList(),
			};
		}

		private static Account Map(GhostfolioSidekick.Model.Accounts.Account account, int arg2)
		{
			return new Account
			{
				Name = account.Name,
				Comment = account.Comment,
				Platform = Map(account.Platform),
				Balance = account.Balance.Select(Map).ToList(),
			};
		}

		private static Platform? Map(GhostfolioSidekick.Model.Accounts.Platform? platform)
		{
			if (platform == null)
			{
				return null;
			}

			return new Platform
			{
				Name = platform.Name,
				Url	= platform.Url,
			};
		}

		private static Balance Map(GhostfolioSidekick.Model.Accounts.Balance balance)
		{
			return new Balance
			{
				Money = Map(balance.Money),
				Date = balance.Date,
			};
		}

		private static Model.Money Map(GhostfolioSidekick.Model.Money money)
		{
			return new Money { Currency = Map(money.Currency) };
		}

		private static Currency Map(GhostfolioSidekick.Model.Currency currency)
		{
			return new Currency { Symbol = currency.Symbol };
		}
	}
}
