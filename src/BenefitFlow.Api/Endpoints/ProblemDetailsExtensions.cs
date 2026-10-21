using BenefitFlow.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BenefitFlow.Api.Endpoints;

internal static class ProblemDetailsExtensions
{
    public static IResult ToProblemDetails(this DomainError error)
    {
        var statusCode = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : error.Code.EndsWith("_conflict", StringComparison.Ordinal) ||
              error.Code is "beneficiary.inactive" or "balance.insufficient"
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status422UnprocessableEntity;

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}
