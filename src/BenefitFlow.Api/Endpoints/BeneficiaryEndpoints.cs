using BenefitFlow.Application.Balances.Queries;
using BenefitFlow.Application.DTOs;
using MediatR;

namespace BenefitFlow.Api.Endpoints;

public static class BeneficiaryEndpoints
{
    public static RouteGroupBuilder MapBeneficiaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/beneficiaries")
            .WithTags("Beneficiaries");

        group.MapGet("/{beneficiaryId:guid}/balance", GetBalanceAsync)
            .WithName("GetBenefitBalance")
            .WithSummary("Gets the available benefit balance for a beneficiary and benefit type.")
            .Produces<BenefitBalanceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetBalanceAsync(
        Guid beneficiaryId,
        BenefitFlow.Domain.Enums.BenefitType benefitType,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBenefitBalanceQuery(beneficiaryId, benefitType),
            cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error!.ToProblemDetails();
    }
}
