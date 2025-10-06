namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public interface ITokenValidationService
	{
		Task<bool> ValidateTokenAsync(string token);
	}
}