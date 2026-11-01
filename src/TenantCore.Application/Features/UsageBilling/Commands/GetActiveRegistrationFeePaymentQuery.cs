using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// Returns the clinic's most recent registration fee payment (open or expired), or null when none
/// has ever been created. Used by the locked-clinic screen so the clinic sees an existing link
/// immediately without having to re-create one.
/// </summary>
public sealed record GetActiveRegistrationFeePaymentQuery(Guid ApplicationId)
    : IRequest<RegistrationFeePaymentDto?>;
