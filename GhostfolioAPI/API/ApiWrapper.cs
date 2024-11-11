using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Symbols;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ApiWrapper(RestCall restCall) : IApiWrapper
	{
		public async Task<List<SymbolProfile>> GetSymbolProfile(string identifier, bool includeIndexes)
		{
			var content = await restCall.DoRestGet(
				$"api/v1/symbol/lookup?query={identifier.Trim()}&includeIndices={includeIndexes.ToString().ToLowerInvariant()}");
			if (content == null)
			{
				return [];
			}

			var symbolProfileList = JsonConvert.DeserializeObject<Contract.SymbolProfileList>(content);
			var assets = symbolProfileList!.Items.Select(ContractToModelMapper.MapSymbolProfile).ToList();
			return assets;
		}
	}
}
