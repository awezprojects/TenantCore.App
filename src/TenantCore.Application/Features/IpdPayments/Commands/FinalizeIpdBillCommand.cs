using MediatR;
using TenantCore.Application.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Commands;

// Internal sub-command dispatched by DischargePatientHandler — not its own API endpoint.
// Locks the bill as-of the discharge moment; a null return means billing is disabled for this
// clinic (no IpdPayment exists), which is not an error — discharge proceeds either way.
public sealed record FinalizeIpdBillCommand(
    Guid IpdRegistrationId,
    Guid ApplicationId,
    DateTime DischargeDate) : IRequest<IpdFinalBillDto?>, IActionLogContext
{
    public string? ActionLogContext => $"ipdRegistrationId={IpdRegistrationId}";
}
