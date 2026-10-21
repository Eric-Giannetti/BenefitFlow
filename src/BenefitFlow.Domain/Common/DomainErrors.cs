namespace BenefitFlow.Domain.Common;

public static class DomainErrors
{
    public static readonly DomainError InvalidCpf = new("beneficiary.invalid_cpf", "CPF is invalid.");
    public static readonly DomainError InvalidBeneficiaryIdentity = new("beneficiary.invalid_identity", "Beneficiary identifier and CPF are required.");
    public static readonly DomainError InvalidBeneficiaryName = new("beneficiary.invalid_name", "Name must contain between 1 and 200 characters.");
    public static readonly DomainError BeneficiaryAlreadyActive = new("beneficiary.already_active", "Beneficiary is already active.");
    public static readonly DomainError BeneficiaryAlreadyInactive = new("beneficiary.already_inactive", "Beneficiary is already inactive.");
    public static readonly DomainError InvalidMoney = new("money.invalid_amount", "Amount must be zero or greater and currency must be a three-letter ISO code.");
    public static readonly DomainError InvalidBenefitType = new("benefit.invalid_type", "Benefit type is invalid.");
    public static readonly DomainError InvalidReservation = new("reservation.invalid", "Reservation details are invalid.");
    public static readonly DomainError InvalidRejectionReason = new("reservation.invalid_rejection_reason", "A rejection reason is required.");
    public static readonly DomainError InvalidReservationState = new("reservation.invalid_state", "Reservation cannot transition from its current status.");
    public static readonly DomainError ReservationNotExpired = new("reservation.not_expired", "Reservation has not reached its expiration time.");
    public static readonly DomainError InvalidBalance = new("balance.invalid", "Balance details are invalid.");
    public static readonly DomainError InsufficientBalance = new("balance.insufficient", "Available balance is insufficient.");
    public static readonly DomainError CurrencyMismatch = new("money.currency_mismatch", "Money values must use the same currency.");
    public static readonly DomainError NegativeBalance = new("balance.negative", "The operation would result in a negative balance.");
    public static readonly DomainError BalanceOverflow = new("balance.overflow", "The operation would exceed the supported balance amount.");
}
