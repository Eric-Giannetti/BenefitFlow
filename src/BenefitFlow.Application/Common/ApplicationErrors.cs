using BenefitFlow.Domain.Common;

namespace BenefitFlow.Application.Common;

public static class ApplicationErrors
{
    public static readonly DomainError BeneficiaryNotFound =
        new("beneficiary.not_found", "Beneficiary was not found.");

    public static readonly DomainError BeneficiaryInactive =
        new("beneficiary.inactive", "Beneficiary is inactive.");

    public static readonly DomainError ReservationNotFound =
        new("reservation.not_found", "Reservation was not found.");

    public static readonly DomainError BalanceNotFound =
        new("balance.not_found", "Benefit balance was not found.");

    public static readonly DomainError IdempotencyKeyConflict =
        new("reservation.idempotency_conflict", "Idempotency key was already used for a different request.");
}
