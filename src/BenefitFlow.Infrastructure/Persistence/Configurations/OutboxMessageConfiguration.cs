using BenefitFlow.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitFlow.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventType).HasMaxLength(512).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.OccurredAt).IsRequired();
        builder.Property(message => message.TraceParent).HasMaxLength(256);
        builder.Property(message => message.TraceState).HasMaxLength(512);
        builder.Property(message => message.ProcessedAt);
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.OccurredAt });
    }
}
