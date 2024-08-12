using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model.Activities
{
	public abstract record Activity
	{
		protected Activity()
		{
			// EF Core
			Account = null!;
		}

		protected Activity(Account account, DateTime date, string? transactionId, int? sortingPriority, string? description)
		{
			Account = account;
			Date = date;
			TransactionId = transactionId;
			SortingPriority = sortingPriority;
			Description = description;
		}

		public string? Id { get; protected set; }

		public Account Account { get; protected set; }

		public DateTime Date { get; protected set; }

		public string? TransactionId { get; protected set; }

		public int? SortingPriority { get; protected set; }

		public string? Description { get; protected set; }

		public async Task<bool> AreEqual(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			if (GetType() != otherActivity.GetType())
			{
				return false;
			}

			var baseEquals =
				Account?.Id == otherActivity.Account?.Id &&
				Date == otherActivity.Date &&
				(Description == null || Description == "<EMPTY>" || Description == otherActivity.Description); // We do not create descriptions when Ghostfolio will ignore them

			if (!baseEquals)
			{
				return false;
			}

			return await AreEqualInternal(exchangeRateService, otherActivity);
		}

		protected abstract Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity);
	}
}
