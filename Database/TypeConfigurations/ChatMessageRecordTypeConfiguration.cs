using GhostfolioSidekick.Model.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	public class ChatMessageRecordTypeConfiguration : IEntityTypeConfiguration<ChatMessageRecord>
	{
		public void Configure(EntityTypeBuilder<ChatMessageRecord> builder)
		{
			builder.HasKey(x => x.Id);
			builder.Property(x => x.ConversationId).IsRequired().HasMaxLength(50);
			builder.HasIndex(x => new { x.ConversationId, x.OrderIndex }).IsUnique();
		}
	}
}
