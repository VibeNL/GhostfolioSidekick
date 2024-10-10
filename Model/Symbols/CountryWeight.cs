namespace GhostfolioSidekick.Model.Symbols
{
	public class CountryWeight
	{
		public CountryWeight(
			string name,
			string code,
			string continent,
			decimal weight)
		{
			Code = code;
			Weight = weight;
			Continent = continent;
			Name = name;
		}

		public string Code { get; set; }

		public decimal Weight { get; set; }

		public string Continent { get; set; }

		public string Name { get; set; }
	}
}