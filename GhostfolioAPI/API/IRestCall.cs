namespace GhostfolioSidekick.GhostfolioAPI.API;

public interface IRestCall
{
	Task<string?> DoRestGet(string suffixUrl, bool useCircuitBreaker = false);
	Task<RestSharp.RestResponse> DoRestPost(string suffixUrl, string body);
	Task<RestSharp.RestResponse> DoRestPut(string suffixUrl, string body);
	Task<RestSharp.RestResponse> DoRestPatch(string suffixUrl, string body);
	Task<RestSharp.RestResponse> DoRestDelete(string suffixUrl);
}
