using System.Security.Claims;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(string token);
        Task LogoutAsync();
        Task<ClaimsPrincipal> GetAuthenticationStateAsync();
        event Action<ClaimsPrincipal> AuthenticationStateChanged;
    }
}