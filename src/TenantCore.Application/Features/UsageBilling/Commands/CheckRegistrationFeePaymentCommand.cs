using MediatR;
using TenantCore.Application.Common;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// "I've paid the registration fee — check now". Same rule as every other check: the task re-reads
/// the link's real status with the gateway, and marking the fee charged stays once-only.
/// </summary>
public sealed record CheckRegistrationFeePaymentCommand(Guid ApplicationId, Guid PaymentId)
    : IRequest, IBusinessAction, IActionLogContext
{
    public string ActionName => "Registration Fee Payment Checked";

    public string ActionLogContext => $"applicationId={ApplicationId}; paymentId={PaymentId}";
}
