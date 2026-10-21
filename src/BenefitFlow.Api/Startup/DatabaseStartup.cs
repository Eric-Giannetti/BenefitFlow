using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using BenefitFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Api.Startup;

public static class DatabaseStartup
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Database:ApplyMigrations"))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BenefitFlowDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!configuration.GetValue<bool>("Database:SeedDemoData") ||
            await dbContext.Beneficiaries.AnyAsync(cancellationToken))
        {
            return;
        }

        var beneficiaryId = Guid.Parse("f87d30ea-8806-4e18-b292-0e03a52178dd");
        var cpf = Cpf.Create("52998224725");
        var initialAmount = Money.Create(500m);
        var beneficiary = Beneficiary.Create(beneficiaryId, "Ana Silva", cpf.Value);
        var balance = BenefitBalance.Open(
            Guid.Parse("ac3e4058-cc34-4a6c-9a44-a0c31136464c"),
            beneficiaryId,
            BenefitType.Consultation,
            initialAmount.Value);

        dbContext.Beneficiaries.Add(beneficiary.Value!);
        dbContext.BenefitBalances.Add(balance.Value!);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
