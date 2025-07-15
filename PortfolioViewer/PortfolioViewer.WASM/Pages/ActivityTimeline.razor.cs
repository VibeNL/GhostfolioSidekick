using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class ActivityTimeline
    {
        [Inject]
        private DatabaseContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private string selectedActivityType = "";
        private string selectedAccount = "";
        private DateTime? dateFrom;
        private DateTime? dateTo;

        private List<ActivityTimelineItem>? AllActivities;
        private List<ActivityTimelineItem>? FilteredActivities;
        private List<string>? ActivityTypes;
        private List<string>? Accounts;
        private ActivityDetailsData? SelectedActivityDetails;

        protected override async Task OnInitializedAsync()
        {
            await LoadTimelineData();
            isLoading = false;
        }

        private async Task LoadTimelineData()
        {
            var activities = await DbContext.Activities
                .Include(a => a.Account)
                .Include(a => a.Holding)
                    .ThenInclude(h => h.SymbolProfiles)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            AllActivities = activities.Select(a => new ActivityTimelineItem
            {
                Id = a.Id,
                Date = a.Date,
                ActivityType = GetActivityTypeName(a),
                Account = a.Account.Name,
                Symbol = GetSymbolFromActivity(a),
                Amount = GetAmountFromActivity(a),
                Description = a.Description ?? ""
            }).ToList();

            ActivityTypes = AllActivities.Select(a => a.ActivityType).Distinct().OrderBy(t => t).ToList();
            Accounts = AllActivities.Select(a => a.Account).Distinct().OrderBy(a => a).ToList();

            FilteredActivities = AllActivities;
        }

        private Task ApplyFilters()
        {
            var filtered = AllActivities?.AsQueryable();

            if (!string.IsNullOrEmpty(selectedActivityType))
            {
                filtered = filtered?.Where(a => a.ActivityType == selectedActivityType);
            }

            if (!string.IsNullOrEmpty(selectedAccount))
            {
                filtered = filtered?.Where(a => a.Account == selectedAccount);
            }

            if (dateFrom.HasValue)
            {
                filtered = filtered?.Where(a => a.Date.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                filtered = filtered?.Where(a => a.Date.Date <= dateTo.Value.Date);
            }

            FilteredActivities = filtered?.ToList() ?? new List<ActivityTimelineItem>();
            StateHasChanged();
            return Task.CompletedTask;
        }

        private Task ClearFilters()
        {
            selectedActivityType = "";
            selectedAccount = "";
            dateFrom = null;
            dateTo = null;
            FilteredActivities = AllActivities;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task ShowActivityDetails(ActivityTimelineItem timelineItem)
        {
            var activity = await DbContext.Activities
                .Include(a => a.Account)
                .Include(a => a.Holding)
                    .ThenInclude(h => h.SymbolProfiles)
                .FirstOrDefaultAsync(a => a.Id == timelineItem.Id);

            if (activity != null)
            {
                SelectedActivityDetails = new ActivityDetailsData
                {
                    TransactionId = activity.TransactionId,
                    Date = activity.Date,
                    ActivityType = GetActivityTypeName(activity),
                    Account = activity.Account.Name,
                    Symbol = GetSymbolFromActivity(activity),
                    Amount = GetAmountFromActivity(activity),
                    Quantity = GetQuantityFromActivity(activity),
                    UnitPrice = GetUnitPriceFromActivity(activity),
                    SortingPriority = activity.SortingPriority,
                    Description = activity.Description ?? "",
                    AdditionalInfo = GetAdditionalInfoFromActivity(activity)
                };
            }
            StateHasChanged();
        }

        private void CloseActivityDetails()
        {
            SelectedActivityDetails = null;
            StateHasChanged();
        }

        private string GetActivityTypeName(Activity activity)
        {
            return activity.GetType().Name switch
            {
                nameof(BuySellActivity) => "Buy/Sell",
                nameof(DividendActivity) => "Dividend",
                nameof(CashDepositWithdrawalActivity) => "Cash Deposit/Withdrawal",
                nameof(FeeActivity) => "Fee",
                nameof(InterestActivity) => "Interest",
                nameof(GiftAssetActivity) => "Gift Asset",
                nameof(GiftFiatActivity) => "Gift Fiat",
                nameof(KnownBalanceActivity) => "Known Balance",
                nameof(LiabilityActivity) => "Liability",
                nameof(RepayBondActivity) => "Repay Bond",
                nameof(ValuableActivity) => "Valuable",
                nameof(SendAndReceiveActivity) => "Send/Receive",
                nameof(StakingRewardActivity) => "Staking Reward",
                _ => activity.GetType().Name.Replace("Activity", "")
            };
        }

        private string GetSymbolFromActivity(Activity activity)
        {
            return activity.Holding?.SymbolProfiles.FirstOrDefault()?.Symbol ?? "";
        }

        private string GetAmountFromActivity(Activity activity)
        {
            return activity switch
            {
                BuySellActivity bsa => bsa.TotalTransactionAmount.ToString(),
                DividendActivity da => da.Amount.ToString(),
                CashDepositWithdrawalActivity cdwa => cdwa.Amount.ToString(),
                FeeActivity fa => fa.Amount.ToString(),
                InterestActivity ia => ia.Amount.ToString(),
                GiftFiatActivity gfa => gfa.Amount.ToString(),
                KnownBalanceActivity kba => kba.Amount.ToString(),
                LiabilityActivity la => la.Price.ToString(),
                RepayBondActivity rba => rba.TotalRepayAmount.ToString(),
                ValuableActivity va => va.Price.ToString(),
                _ => ""
            };
        }

        private string GetQuantityFromActivity(Activity activity)
        {
            return activity switch
            {
                ActivityWithQuantityAndUnitPrice qup => qup.Quantity.ToString("N4"),
                _ => ""
            };
        }

        private string GetUnitPriceFromActivity(Activity activity)
        {
            return activity switch
            {
                ActivityWithQuantityAndUnitPrice qup => qup.UnitPrice.ToString(),
                _ => ""
            };
        }

        private Dictionary<string, string> GetAdditionalInfoFromActivity(Activity activity)
        {
            var info = new Dictionary<string, string>();

            if (activity is BuySellActivity bsa)
            {
                info["Adjusted Quantity"] = bsa.AdjustedQuantity.ToString("N4");
                info["Adjusted Unit Price"] = bsa.AdjustedUnitPrice.ToString();
                if (bsa.Fees.Any())
                {
                    info["Total Fees"] = string.Join(", ", bsa.Fees.Select(f => f.Money.ToString()));
                }
                if (bsa.Taxes.Any())
                {
                    info["Total Taxes"] = string.Join(", ", bsa.Taxes.Select(t => t.Money.ToString()));
                }
            }
            else if (activity is DividendActivity da)
            {
                if (da.Fees.Any())
                {
                    info["Total Fees"] = string.Join(", ", da.Fees.Select(f => f.Money.ToString()));
                }
                if (da.Taxes.Any())
                {
                    info["Total Taxes"] = string.Join(", ", da.Taxes.Select(t => t.Money.ToString()));
                }
            }
            else if (activity is SendAndReceiveActivity sra)
            {
                if (sra.Fees.Any())
                {
                    info["Total Fees"] = string.Join(", ", sra.Fees.Select(f => f.Money.ToString()));
                }
            }

            return info;
        }

        private string GetActivityBadgeClass(string activityType)
        {
            return activityType switch
            {
                "Buy/Sell" => "bg-primary",
                "Dividend" => "bg-success",
                "Cash Deposit/Withdrawal" => "bg-info",
                "Fee" => "bg-warning text-dark",
                "Interest" => "bg-secondary",
                "Gift Asset" => "bg-success",
                "Gift Fiat" => "bg-info",
                "Known Balance" => "bg-dark",
                "Liability" => "bg-danger",
                "Repay Bond" => "bg-primary",
                "Valuable" => "bg-warning text-dark",
                "Send/Receive" => "bg-info",
                "Staking Reward" => "bg-success",
                _ => "bg-light text-dark"
            };
        }

        private string GetTimelineBorderClass(string activityType)
        {
            return activityType switch
            {
                "Buy/Sell" => "border-primary",
                "Dividend" => "border-success",
                "Cash Deposit/Withdrawal" => "border-info",
                "Fee" => "border-warning",
                "Interest" => "border-secondary",
                "Gift Asset" => "border-success",
                "Gift Fiat" => "border-info",
                "Known Balance" => "border-dark",
                "Liability" => "border-danger",
                "Repay Bond" => "border-primary",
                "Valuable" => "border-warning",
                "Send/Receive" => "border-info",
                "Staking Reward" => "border-success",
                _ => "border-light"
            };
        }

        private class ActivityTimelineItem
        {
            public long Id { get; set; }
            public DateTime Date { get; set; }
            public string ActivityType { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Amount { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private class ActivityDetailsData
        {
            public string TransactionId { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public string ActivityType { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Amount { get; set; } = string.Empty;
            public string Quantity { get; set; } = string.Empty;
            public string UnitPrice { get; set; } = string.Empty;
            public int? SortingPriority { get; set; }
            public string Description { get; set; } = string.Empty;
            public Dictionary<string, string> AdditionalInfo { get; set; } = new();
        }
    }
}