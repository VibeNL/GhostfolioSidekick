using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages;

public partial class HoldingsPriceTargets
{
	private List<HoldingPriceTargetDisplayModel> _holdingsPriceTargetsList = [];
	private bool _isLoading = true;
	private bool _hasError = false;
	private string _errorMessage = string.Empty;

	protected override async Task OnInitializedAsync()
	{
		await LoadDataAsync();
	}

	private async Task LoadDataAsync()
	{
		_isLoading = true;
		_hasError = false;
		try
		{
			_holdingsPriceTargetsList = await PriceTargetsService.GetHoldingsPriceTargetsAsync();
		}
		catch (Exception ex)
		{
			_hasError = true;
			_errorMessage = ex.Message;
		}
		finally
		{
			_isLoading = false;
		}
	}

	private async Task RefreshDataAsync()
	{
		await LoadDataAsync();
	}

	private static string FormatPrice(decimal amount, string currency)
	{
		return $"{amount:N2} {currency}";
	}

	private static string GetProximityBadge(decimal proximityPercentage)
	{
		var badgeClass = proximityPercentage switch
		{
			>= 100 => "bg-success",
			>= 90 => "bg-info",
			_ => "bg-secondary"
		};
		return $"<span class=\"badge {badgeClass}\">{proximityPercentage:N2}%</span>";
	}

	private static string GetRatingBadge(string rating)
	{
		var lower = rating?.ToLowerInvariant() ?? "unknown";
		return lower switch
		{
			"strongbuy" => "<span class=\"badge bg-success\">Strong Buy</span>",
			"buy" => "<span class=\"badge bg-info\">Buy</span>",
			"hold" => "<span class=\"badge bg-warning text-dark\">Hold</span>",
			"sell" => "<span class=\"badge bg-danger\">Sell</span>",
			"strongsell" => "<span class=\"badge bg-dark\">Strong Sell</span>",
			_ => "<span class=\"badge bg-secondary\">N/A</span>"
		};
	}
}
