using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
	public partial class LoginDisplay : ComponentBase
	{
		[Inject] private NavigationManager Navigation { get; set; } = default!;
		[Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;

		public async Task BeginLogOut()
		{
			await AuthenticationService.LogoutAsync();
			Navigation.NavigateTo("/login", replace: true);
		}
	}
}