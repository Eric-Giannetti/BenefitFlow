using BenefitFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitFlow.Infrastructure.Persistence.Configurations;

public sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("beneficiaries");
        builder.HasKey(beneficiary => beneficiary.Id);
        builder.Property(beneficiary => beneficiary.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(beneficiary => beneficiary.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Ignore(beneficiary => beneficiary.DomainEvents);

        builder.OwnsOne(beneficiary => beneficiary.Cpf, cpf =>
        {
            cpf.Property(value => value.Value)
                .HasColumnName("cpf")
                .HasMaxLength(11)
                .IsRequired();
            cpf.HasIndex(value => value.Value).IsUnique();
        });

        builder.Navigation(beneficiary => beneficiary.Cpf).IsRequired();
    }
}
