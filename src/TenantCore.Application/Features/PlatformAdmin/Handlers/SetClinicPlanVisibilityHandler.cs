using MediatR;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

public sealed class SetClinicPlanVisibilityHandler(IClinicAccountRepository accountRepository)
    : IRequestHandler<SetClinicPlanVisibilityCommand>
{
    public async Task Handle(SetClinicPlanVisibilityCommand command, CancellationToken ct)
    {
        var account = await accountRepository.GetByApplicationIdAsync(command.ApplicationId, ct);

        if (account is null)
        {
            account = ClinicAccount.CreateDefault(command.ApplicationId);
            await accountRepository.AddAsync(account, ct);
        }

        account.SetRestrictToOfferedPlans(command.RestrictToOfferedPlans);
        await accountRepository.SaveChangesAsync(ct);
    }
}
