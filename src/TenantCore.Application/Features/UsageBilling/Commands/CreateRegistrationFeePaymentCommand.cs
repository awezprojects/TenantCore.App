using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// A Clinic Admin paying the clinic's one-time registration fee. The fee is never ridden inside a
/// plan payment: it has to work for a pay-as-you-go clinic too, which has no plan payment at all.
///
/// The amount is resolved server-side from the clinic's override or the plan's catalogue default —
/// a caller-supplied amount would make the fee negotiable by the party that owes it.
/// </summary>
public sealed record CreateRegistrationFeePaymentCommand(Guid ApplicationId, Guid ActingUserId)
    : IRequest<RegistrationFeePaymentDto>, IBusinessAction, IActionLogContext
{
    public string ActionName => "Registration Fee Payment Requested";

    public string ActionLogContext => $"applicationId={ApplicationId}; userId={ActingUserId}";
}
