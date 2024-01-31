using System.Runtime.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public enum MappingType
	{
		[EnumMember(Value = "currency")]
		Currency,

		[EnumMember(Value = "symbol")]
		Symbol
	}
}