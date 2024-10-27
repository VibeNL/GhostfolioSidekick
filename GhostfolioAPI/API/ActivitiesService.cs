using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ActivitiesService : IActivitiesService
	{
		/// <summary>
		/// /private readonly ICurrencyExchange exchangeRateService;
		/// </summary>
		private readonly ILogger<ActivitiesService> logger;
		private readonly RestCall restCall;
		private readonly IAccountService accountService;

		public ActivitiesService(
				////ICurrencyExchange exchangeRateService,
				IAccountService accountService,
				RestCall restCall,
				ILogger<ActivitiesService> logger)
		{
			////this.exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
			this.accountService = accountService;
		}

		public async Task<IEnumerable<Model.Activities.Activity>> GetAllActivities()
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private Task WriteOrder(Model.Activities.Activity activity)
		{
			throw new NotImplementedException();
			////if (activity.Type == ActivityType.IGNORE)
			////{
			////	logger.LogTrace("Skipping ignore transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);

			////	return;
			////}

			////var url = $"api/v1/order";
			////await restCall.DoRestPost(url, await ConvertToBody(activity));

			////logger.LogDebug("Added transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private async Task DeleteOrder(Contract.Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.Id))
			{
				throw new NotSupportedException($"Deletion failed, no Id");
			}

			await restCall.DoRestDelete($"api/v1/order/{activity.Id}");
			logger.LogDebug("Deleted transaction {Date} {Symbol} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Type);
		}

		////private static Task<string> ConvertToBody(Contract.Activity activity)
		////{
		////	var o = new JObject
		////	{
		////		["accountId"] = activity.AccountId,
		////		["comment"] = activity.Comment,
		////		["currency"] = activity.SymbolProfile?.Currency,
		////		["dataSource"] = activity.SymbolProfile?.DataSource,
		////		["date"] = activity.Date.ToString("o"),
		////		["fee"] = activity.Fee,
		////		["quantity"] = activity.Quantity,
		////		["symbol"] = activity.SymbolProfile?.Symbol,
		////		["type"] = activity.Type.ToString(),
		////		["unitPrice"] = activity.UnitPrice
		////	};
		////	var res = o.ToString();
		////	return Task.FromResult(res);
		////}

		public Task InsertActivity(Model.Symbols.SymbolProfile symbolProfile, Model.Activities.Activity activity)
		{
			throw new NotImplementedException();
			////var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			////await WriteOrder(converted);
		}

		public Task UpdateActivity(Model.Symbols.SymbolProfile symbolProfile, Model.Activities.Activity oldActivity, Model.Activities.Activity newActivity)
		{
			throw new NotImplementedException();
			////var oldActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, oldActivity);
			////var newActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, newActivity);

			////await DeleteOrder(oldActivityConverted);
			////await WriteOrder(newActivityConverted);
		}

		public Task DeleteActivity(Model.Symbols.SymbolProfile symbolProfile, Model.Activities.Activity activity)
		{
			throw new NotImplementedException();
			////	var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			////	await DeleteOrder(converted);
		}
	}
}
