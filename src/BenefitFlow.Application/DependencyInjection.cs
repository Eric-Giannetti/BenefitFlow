using BenefitFlow.Application.Behaviors;
using BenefitFlow.Application.Mapping;
using BenefitFlow.Application.Observability;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BenefitFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton<ReservationMetrics>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        var mappingConfiguration = new TypeAdapterConfig();
        ApplicationMappingConfiguration.Configure(mappingConfiguration);
        services.AddSingleton(mappingConfiguration);

        return services;
    }
}
