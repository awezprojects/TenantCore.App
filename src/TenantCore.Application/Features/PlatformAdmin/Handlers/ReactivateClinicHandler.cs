using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

public sealed class ReactivateClinicHandler(
    IClinicAccountRepository accountRepository,
    ILogger<ReactivateClinicHandler> logger)
    : IRequestHandler<ReactivateClinicCommand>
{
    public async Task Handle(ReactivateClinicCommand command, CancellationToken ct)
    {
        var account = await accountRepository.GetByApplicationIdAsync(command.ApplicationId, ct);

        // No row at all means the clinic was never suspended — same answer as an active row (409),
        // rather than silently creating one just to report "not suspended".
        if (account is null)
            throw new InvalidOperationException("This clinic is not suspended.");

        account.Reactivate(command.AdminEmail);
        await accountRepository.SaveChangesAsync(ct);

        logger.LogInformation("Clinic {ApplicationId} was reactivated by an internal admin.", command.ApplicationId);
    }
}
