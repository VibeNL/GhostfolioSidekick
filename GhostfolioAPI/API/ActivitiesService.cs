﻿using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Activity = GhostfolioSidekick.Model.Activities.Activity;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ActivitiesService : IActivitiesService
	{
		private readonly IExchangeRateService exchangeRateService;
		private readonly ILogger<ActivitiesService> logger;
		private readonly RestCall restCall;
		private readonly IAccountService accountService;

		public ActivitiesService(
				IExchangeRateService exchangeRateService,
				IAccountService accountService,
				RestCall restCall,
				ILogger<ActivitiesService> logger)
		{
			this.exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
			this.accountService = accountService;
		}

		public async Task<IEnumerable<Holding>> GetAllActivities()
		{
			var content = await restCall.DoRestGet($"api/v1/order");
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities;

			var accounts = await accountService.GetAllAccounts();

			return ContractToModelMapper.MapToHoldings(accounts, existingActivities);
		}

		public Task DeleteAll()
		{
			return restCall.DoRestDelete($"api/v1/order/");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "Cleaner")]
		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
				return;
			}

			if (activity.Type == Contract.ActivityType.IGNORE)
			{
				logger.LogTrace($"Skipping ignore transaction {activity.Date} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
				return;
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
			if (string.IsNullOrWhiteSpace(order.Id))
			{
				throw new NotSupportedException($"Deletion failed, no Id");
			}

			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Type} {order.SymbolProfile?.Symbol} {order.Date}");
		}

		private Task<string> ConvertToBody(Contract.Activity activity)
		{
			var o = new JObject();
			o["accountId"] = activity.AccountId;
			o["comment"] = activity.Comment;
			o["currency"] = activity.SymbolProfile?.Currency;
			o["dataSource"] = activity.SymbolProfile?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;
			o["symbol"] = activity.SymbolProfile?.Symbol;
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return Task.FromResult(res);
		}

		public async Task InsertActivity(Model.Symbols.SymbolProfile symbolProfile, Activity activity)
		{
			var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			await WriteOrder(converted);
		}

		public async Task UpdateActivity(Model.Symbols.SymbolProfile symbolProfile, Activity oldActivity, Activity newActivity)
		{
			var oldActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, oldActivity);
			var newActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, newActivity);

			await DeleteOrder(oldActivityConverted);
			await WriteOrder(newActivityConverted);
		}

		public async Task DeleteActivity(Model.Symbols.SymbolProfile symbolProfile, Activity activity)
		{
			var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			await DeleteOrder(converted);
		}
	}
}
