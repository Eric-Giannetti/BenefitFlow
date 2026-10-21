using BenefitFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitFlow.Infrastructure.Persistence.Configurations;

public sealed class BenefitReservationConfiguration : IEntityTypeConfiguration<BenefitReservation>
{
    public void Configure(EntityTypeBuilder<BenefitReservation> builder)
    {
        builder.ToTable("benefit_reservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.BeneficiaryId).IsRequired();
        builder.Property(reservation => reservation.BenefitType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(reservation => reservation.CreatedAt).IsRequired();
        builder.Property(reservation => reservation.ExpiresAt).IsRequired();
        builder.Property(reservation => reservation.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();
        builder.HasOne<Beneficiary>()
            .WithMany()
            .HasForeignKey(reservation => reservation.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(reservation => reservation.IdempotencyKey).IsUnique();
        builder.HasIndex(reservation => new { reservation.Status, reservation.ExpiresAt });
        builder.Ignore(reservation => reservation.DomainEvents);

        builder.OwnsOne(reservation => reservation.Amount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
            money.Property(value => value.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(reservation => reservation.Amount).IsRequired();
    }
}
