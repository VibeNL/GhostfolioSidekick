using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialActivity(SymbolProfile symbolProfile)
	{
		public SymbolProfile SymbolProfile { get; set; } = symbolProfile;

		public static PartialActivity CreateCashDeposit(Currency currency, decimal v, DateTime date)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateCashWithdrawal(Currency currency, decimal v, DateTime date)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateInterest(Currency currency, decimal amount, DateTime date)
		{
			throw new NotImplementedException();
		}
	}
}