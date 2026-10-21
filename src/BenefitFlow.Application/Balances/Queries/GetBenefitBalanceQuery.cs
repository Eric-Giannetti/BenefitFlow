using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using MediatR;

namespace BenefitFlow.Application.Balances.Queries;

public sealed record GetBenefitBalanceQuery(Guid BeneficiaryId, BenefitType BenefitType)
    : IRequest<Result<BenefitBalanceResponse>>;
