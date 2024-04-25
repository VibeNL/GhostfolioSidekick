using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private async Task WriteOrder(Activity activity)
		{
			if (activity.Type == ActivityType.IGNORE)
			{
				logger.LogTrace($"Skipping ignore transaction {activity.Date.ToInvariantString()} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
				return;
			}
			
			var url = $"api/v1/order";
			await restCall.DoRestPost(url, await ConvertToBody(activity));

			logger.LogInformation($"Added transaction {activity.Date.ToInvariantString()} {activity.SymbolProfile?.Symbol} {activity.Quantity} {activity.Type}");
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private async Task DeleteOrder(Contract.Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.Id))
			{
				throw new NotSupportedException($"Deletion failed, no Id");
			}

			await restCall.DoRestDelete($"api/v1/order/{activity.Id}");
			logger.LogInformation($"Deleted transaction  {activity.Date.ToInvariantString()} {activity.SymbolProfile?.Symbol} {activity.Type}");
		}

		private static Task<string> ConvertToBody(Contract.Activity activity)
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

		public async Task InsertActivity(Model.Symbols.SymbolProfile symbolProfile, IActivity activity)
		{
			var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			await WriteOrder(converted);
		}

		public async Task UpdateActivity(Model.Symbols.SymbolProfile symbolProfile, IActivity oldActivity, IActivity newActivity)
		{
			var oldActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, oldActivity);
			var newActivityConverted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, newActivity);

			await DeleteOrder(oldActivityConverted);
			await WriteOrder(newActivityConverted);
		}

		public async Task DeleteActivity(Model.Symbols.SymbolProfile symbolProfile, IActivity activity)
		{
			var converted = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateService, symbolProfile, activity);

			await DeleteOrder(converted);
		}
	}
}
