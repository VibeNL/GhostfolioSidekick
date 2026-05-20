using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhostfolioSidekick.Database.Cache
{
	public class ExternalDataCacheEntry
	{
		[Key]
		public long Id { get; set; }

		[Required]
		public string CacheKey { get; set; } = null!;

		[Required]
		public string DataType { get; set; } = null!; // e.g. "MarketData", "SymbolProfile", "Dividend"

		[Required]
		[Column(TypeName = "BLOB")]
		public byte[] DataJson { get; set; } = null!;

		public DateTime CreatedAt { get; set; }

		public DateTime? ExpiresAt { get; set; }
	}
}
