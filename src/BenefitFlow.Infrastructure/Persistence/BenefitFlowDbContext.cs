using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Infrastructure.Persistence.Outbox;
using BenefitFlow.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Infrastructure.Persistence;

public sealed class BenefitFlowDbContext(DbContextOptions<BenefitFlowDbContext> options)
    : DbContext(options), IUnitOfWork
{
    private readonly Dictionary<IDomainEvent, OutboxMessage> _outboxMessagesByEvent =
        new(ReferenceEqualityComparer.Instance);

    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();

    public DbSet<BenefitReservation> Reservations => Set<BenefitReservation>();

    public DbSet<BenefitBalance> BenefitBalances => Set<BenefitBalance>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BenefitFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregateEntries = ChangeTracker.Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .ToArray();
        var domainEvents = aggregateEntries
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToArray();

        foreach (var domainEvent in domainEvents)
        {
            if (!_outboxMessagesByEvent.TryGetValue(domainEvent, out var outboxMessage))
            {
                outboxMessage = OutboxMessage.From(domainEvent);
                _outboxMessagesByEvent.Add(domainEvent, outboxMessage);
                OutboxMessages.Add(outboxMessage);
            }
        }

        var affectedRows = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in aggregateEntries)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                _outboxMessagesByEvent.Remove(domainEvent);
            }

            entry.Entity.ClearDomainEvents();
        }

        return affectedRows;
    }
}
