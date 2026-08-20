using DataAccess.Infrastructure.EfCore.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Infrastructure.EfCore.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        // The message id is the natural key; its uniqueness is what makes the consumer idempotent
        // even when two deliveries race — the second insert violates the PK and is treated as a dup.
        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId).HasMaxLength(128);
    }
}
