using BenefitFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitFlow.Infrastructure.Persistence.Configurations;

public sealed class BenefitBalanceConfiguration : IEntityTypeConfiguration<BenefitBalance>
{
    public void Configure(EntityTypeBuilder<BenefitBalance> builder)
    {
        builder.ToTable("benefit_balances");
        builder.HasKey(balance => balance.Id);
        builder.Property(balance => balance.BeneficiaryId).IsRequired();
        builder.Property(balance => balance.BenefitType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.HasOne<Beneficiary>()
            .WithMany()
            .HasForeignKey(balance => balance.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(balance => new { balance.BeneficiaryId, balance.BenefitType }).IsUnique();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(balance => balance.DomainEvents);

        builder.OwnsOne(balance => balance.AvailableAmount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("available_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            money.Property(value => value.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(balance => balance.AvailableAmount).IsRequired();
    }
}
