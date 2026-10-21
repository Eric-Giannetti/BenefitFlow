using BenefitFlow.Api.Models;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Application.Balances.Queries;
using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Application.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BenefitFlow.Api.Endpoints;

public static class ReservationEndpoints
{
    public const string CreationRateLimitPolicy = "reservation-create";

    public static RouteGroupBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reservations")
            .WithTags("Reservations");

        group.MapPost("", CreateAsync)
            .RequireRateLimiting(CreationRateLimitPolicy)
            .WithName("CreateReservation")
            .WithSummary("Creates a pending benefit reservation.")
            .WithDescription("Requires the Idempotency-Key header. The operation is processed asynchronously.")
            .Produces<ReservationResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();
        group.MapGet("/{reservationId:guid}", GetAsync)
            .WithName("GetReservation")
            .WithSummary("Gets a reservation and its current status.")
            .Produces<ReservationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();
        group.MapPost("/{reservationId:guid}/cancel", CancelAsync)
            .WithName("CancelReservation")
            .WithSummary("Cancels a confirmed reservation and restores its balance.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreateReservationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "idempotency_key_required",
                detail: "The Idempotency-Key header is required.");
        }

        var result = await sender.Send(
            new CreateReservationCommand(
                request.BeneficiaryId,
                request.BenefitType,
                request.Amount,
                request.Currency,
                idempotencyKey),
            cancellationToken);

        return result.IsSuccess
            ? Results.Accepted($"/api/v1/reservations/{result.Value!.Id}", result.Value)
            : result.Error!.ToProblemDetails();
    }

    private static async Task<IResult> GetAsync(
        Guid reservationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReservationQuery(reservationId), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error!.ToProblemDetails();
    }

    private static async Task<IResult> CancelAsync(
        Guid reservationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelReservationCommand(reservationId), cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error!.ToProblemDetails();
    }
}
