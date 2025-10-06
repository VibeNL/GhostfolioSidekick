using Microsoft.JSInterop;
using System.Security.Claims;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class AuthenticationService : IAuthenticationService
	{
		private const string StorageKey = "authToken";
		private readonly ITokenValidationService _tokenValidationService;
		private readonly IJSRuntime _jsRuntime;
		private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

		public event Action<ClaimsPrincipal>? AuthenticationStateChanged;

		public AuthenticationService(ITokenValidationService tokenValidationService, IJSRuntime jsRuntime)
		{
			_tokenValidationService = tokenValidationService;
			_jsRuntime = jsRuntime;
		}

		public async Task<bool> LoginAsync(string token)
		{
			try
			{
				// Validate the token
				var isValid = await _tokenValidationService.ValidateTokenAsync(token);

				if (isValid)
				{
					// Store the token
					await StoreTokenAsync(token);

					// Create authenticated user
					var identity = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.Name, "Authenticated User"),
						new Claim(ClaimTypes.Authentication, "true")
					}, "tokenAuth");

					_currentUser = new ClaimsPrincipal(identity);
					AuthenticationStateChanged?.Invoke(_currentUser);

					return true;
				}
			}
			catch (Exception)
			{
				// Token validation failed
			}

			return false;
		}

		public async Task LogoutAsync()
		{
			await RemoveTokenAsync();
			_currentUser = new ClaimsPrincipal(new ClaimsIdentity());
			AuthenticationStateChanged?.Invoke(_currentUser);
		}

		public async Task<ClaimsPrincipal> GetAuthenticationStateAsync()
		{
			if (_currentUser.Identity?.IsAuthenticated == true)
			{
				return _currentUser;
			}

			// Try to restore from storage
			var token = await GetStoredTokenAsync();
			if (!string.IsNullOrEmpty(token))
			{
				var isValid = await _tokenValidationService.ValidateTokenAsync(token);
				if (isValid)
				{
					var identity = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.Name, "Authenticated User"),
						new Claim(ClaimTypes.Authentication, "true")
					}, "tokenAuth");

					_currentUser = new ClaimsPrincipal(identity);
					return _currentUser;
				}
				else
				{
					// Invalid stored token, remove it
					await RemoveTokenAsync();
				}
			}

			return new ClaimsPrincipal(new ClaimsIdentity());
		}

		private async Task StoreTokenAsync(string token)
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
			}
			catch
			{
				// Fallback - could implement in-memory storage
			}
		}

		private async Task<string?> GetStoredTokenAsync()
		{
			try
			{
				return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
			}
			catch
			{
				return null;
			}
		}

		private async Task RemoveTokenAsync()
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
			}
			catch
			{
				// Ignore errors when removing
			}
		}
	}
}