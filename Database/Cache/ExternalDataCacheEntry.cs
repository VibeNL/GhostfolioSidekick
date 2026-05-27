using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhostfolioSidekick.Database.Cache
{
	public class ExternalDataCacheEntry
	{
		[Key]
		public long Id { get; set; }

		[Required]
		public string Key { get; set; } = null!;

		[Required]
		[Column(TypeName = "BLOB")]
		public byte[] DataJson { get; set; } = null!;

		public DateTime CreatedAt { get; set; }

		public DateTime ExpiresAt { get; set; }
	}
}
