using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin waiving the one-time registration fee. The clinic is never asked for it again, and
/// a future payment attempt is refused by the domain rather than by a check here — the row is the
/// only place this decision can live.
/// </summary>
public sealed class WaiveRegistrationFeeHandler(
    IClinicAccountRepository accountRepository,
    ILogger<WaiveRegistrationFeeHandler> logger)
    : IRequestHandler<WaiveRegistrationFeeCommand>
{
    public async Task Handle(WaiveRegistrationFeeCommand command, CancellationToken ct)
    {
        var account = await accountRepository.GetOrCreateAsync(command.ApplicationId, ct);

        // The domain owns this rule: a fee that is already paid cannot be waived (that needs a
        // refund, not a flag), and a second waiver is refused rather than silently accepted.
        account.WaiveRegistrationFee(command.Reason, command.AdminEmail);

        await accountRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminEmail} waived the registration fee for clinic {ApplicationId}. Reason: {Reason}.",
            command.AdminEmail, command.ApplicationId, command.Reason);
    }
}
