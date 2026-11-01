using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Locks a clinic out of the whole app. The account row is created on demand, so a clinic that has
/// never needed one (the normal case) does not have to be backfilled first.
/// </summary>
public sealed class SuspendClinicHandler(
    IClinicAccountRepository accountRepository,
    ILogger<SuspendClinicHandler> logger)
    : IRequestHandler<SuspendClinicCommand>
{
    public async Task Handle(SuspendClinicCommand command, CancellationToken ct)
    {
        var account = await accountRepository.GetByApplicationIdAsync(command.ApplicationId, ct);

        if (account is null)
        {
            account = ClinicAccount.CreateDefault(command.ApplicationId);
            await accountRepository.AddAsync(account, ct);
        }

        // Throws InvalidOperationException (→ 409) when it is already suspended.
        account.Suspend(command.Message, command.AdminEmail);
        await accountRepository.SaveChangesAsync(ct);

        logger.LogWarning("Clinic {ApplicationId} was suspended by an internal admin.", command.ApplicationId);
    }
}
