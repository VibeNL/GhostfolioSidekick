using GhostfolioSidekick.Ghostfolio.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Activity = GhostfolioSidekick.Model.Activities.Activity;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ActivitiesService : IActivitiesService
	{
		private readonly IExchangeRateService exchangeRateService;
		private readonly IAccountService accountService;
		private readonly ILogger<ActivitiesService> logger;
		private readonly RestCall restCall;

		public ActivitiesService(
				IExchangeRateService exchangeRateService,
				IAccountService accountService,
				RestCall restCall,
				ILogger<ActivitiesService> logger)
		{
			this.exchangeRateService = exchangeRateService;
			this.accountService = accountService;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
		}

		public async Task UpdateActivities(List<string> accountNames, IEnumerable<Holding> holdings)
		{
			var activityList = new List<Contract.Activity>();
			foreach (Holding holding in holdings)
				foreach (Activity activity in holding.Activities)
				{
					var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, holding.SymbolProfile, activity);
					if (converted != null)
					{
						activityList.Add(converted);
					}
				}

			var newActivities = activityList
					.Where(x => x.Type != Contract.ActivityType.IGNORE)
					.Select(Round!)
					.Where(x => x.Quantity != 0 || x.Fee != 0)
					.ToList();

			foreach (var accountName in accountNames)
			{
				var existingAccount = await accountService.GetAccountByName(accountName);
				var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.None());

				if (content == null)
				{
					// Account is missing
					continue;
				}

				var existingActivities = JsonConvert.DeserializeObject<Contract.ActivityList>(content)?.Activities ?? [];
				var ordersFromFiles = newActivities.Where(x => x.AccountId == existingAccount.Id).ToList();

				// Update Balance
				var newBalance = await BalanceCalculator.Calculate(
					existingAccount.Balance.Money.Currency,
					exchangeRateService,
					holdings.SelectMany(x => x.Activities).Where(x => x.Account.Name == accountName));
				await accountService.UpdateBalance(existingAccount, newBalance);

				var mergeOrders = MergeOrders(ordersFromFiles, existingActivities)
					.OrderBy(x => x.Order1?.Date ?? x.Order2?.Date ?? DateTime.MaxValue)
					.ThenBy(x => x.Operation)
					.ToList();
				foreach (var mergeOrder in mergeOrders)
				{
					try
					{
						switch (mergeOrder.Operation)
						{
							case Operation.New:
								await WriteOrder(mergeOrder.Order1!);
								break;
							case Operation.Duplicate:
								// Nothing to do!
								break;
							case Operation.Updated:
								await DeleteOrder(mergeOrder.Order2!);
								await WriteOrder(mergeOrder.Order1!);
								break;
							case Operation.Removed:
								await DeleteOrder(mergeOrder.Order2!);
								break;
							default:
								throw new NotSupportedException();
						}
					}
					catch (Exception ex)
					{
						logger.LogError($"Transaction failed to write {ex}, skipping");
					}
				}
			}
		}

		public async Task<IEnumerable<Holding>> GetAllActivities()
		{
			var content = await restCall.DoRestGet($"api/v1/order", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities;

			return ContractToModelMapper.MapToHoldings(existingActivities);
		}

		private static Contract.Activity Round(Contract.Activity activity)
		{
			static decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			}

			activity.Fee = Round(activity.Fee);
			activity.Quantity = Round(activity.Quantity);
			activity.UnitPrice = Round(activity.UnitPrice);

			return activity;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "Cleaner")]
		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == Contract.ActivityType.IGNORE)
			{
				logger.LogDebug($"Skipping ignore transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			var url = $"api/v1/order";
			var r = await restCall.DoRestPost(url, await ConvertToBody(activity));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content?.Equals("{\"activities\":[]}") ?? true))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
			}

			logger.LogInformation($"Added transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
		}

		private async Task DeleteOrder(Contract.Activity order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Type} {order.SymbolProfile?.Symbol} {order.Date}");
		}

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Contract.Activity> ordersFromFiles, IEnumerable<Contract.Activity> existingOrders)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";

			var existingOrdersWithMatchFlag = existingOrders.Select(x => new MatchActivity { Activity = x, IsMatched = false }).ToList();
			return ordersFromFiles.GroupJoin(existingOrdersWithMatchFlag,
				fo => fo.ReferenceCode,
				eo =>
				{
					if (string.IsNullOrWhiteSpace(eo.Activity.Comment))
					{
						return Guid.NewGuid().ToString();
					}

					var match = Regex.Match(eo.Activity.Comment, pattern);
					var key = (match.Groups.Count > 1 ? match.Groups[1]?.Value : null) ?? string.Empty;
					return key;
				},
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (AreEquals(fo, other.Activity))
						{
							return new MergeOrder(Operation.Duplicate, fo);
						}

						return new MergeOrder(Operation.Updated, fo, other.Activity);
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, fo);
					}
					else
					{
						throw new NotSupportedException();
					}
				}).Union(existingOrdersWithMatchFlag.Where(x => !x.IsMatched).Select(x => new MergeOrder(Operation.Removed, null, x.Activity)));
		}

		private bool AreEquals(Contract.Activity fo, Contract.Activity eo)
		{
			return
				(fo.SymbolProfile?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == Contract.ActivityType.INTEREST || fo.Type == Contract.ActivityType.FEE) && // Interest & Fee create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private Task<string> ConvertToBody(Contract.Activity activity)
		{
			var o = new JObject();
			o["accountId"] = activity.AccountId;
			o["comment"] = activity.Comment;
			o["currency"] = activity.Currency;
			o["dataSource"] = activity.SymbolProfile?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;

			if (activity.Type == Contract.ActivityType.INTEREST)
			{
				o["symbol"] = "Interest";
			}
			else if (activity.Type == Contract.ActivityType.FEE)
			{
				o["symbol"] = "Fee";
			}
			else
			{
				o["symbol"] = activity.SymbolProfile?.Symbol;
			}
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return Task.FromResult(res);
		}
	}
}
