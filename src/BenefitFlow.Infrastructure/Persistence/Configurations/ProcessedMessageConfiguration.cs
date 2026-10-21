using BenefitFlow.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitFlow.Infrastructure.Persistence.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(message => message.MessageId);
        builder.Property(message => message.ProcessedAt).IsRequired();
    }
}
