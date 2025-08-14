using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly IAuthenticationService _authenticationService;

        public CustomAuthenticationStateProvider(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = await _authenticationService.GetAuthenticationStateAsync();
            return new AuthenticationState(user);
        }

        private void OnAuthenticationStateChanged(ClaimsPrincipal user)
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void Dispose()
        {
            _authenticationService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}