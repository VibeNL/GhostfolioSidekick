using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public abstract record BaseActivity<T> : IActivity
	{
		public abstract Account Account { get; }

		public abstract DateTime Date { get; }

		public abstract string? TransactionId { get; set; }

		public abstract int? SortingPriority { get; set; }

		public abstract string? Id { get; set; }

		public string? Description { get; set; }

		public async Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity otherActivity)
		{
			if (otherActivity.GetType() != otherActivity.GetType())
			{
				return false;
			}

			var baseEquals =
				Account?.Id == otherActivity.Account?.Id &&
				Date == otherActivity.Date &&
				(Description == null || Description == otherActivity.Description); // We do not create descriptions when Ghostfolio will ignore them

			if (!baseEquals)
			{
				return false;
			}

			return await AreEqualInternal(exchangeRateService, (T)otherActivity);
		}

		protected abstract Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, T otherActivity);
	}
}
