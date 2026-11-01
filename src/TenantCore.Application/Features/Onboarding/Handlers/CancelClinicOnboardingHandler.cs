using MediatR;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Onboarding.Handlers;

public sealed class CancelClinicOnboardingHandler(IClinicOnboardingRequestRepository requestRepository)
    : IRequestHandler<CancelClinicOnboardingCommand>
{
    public async Task Handle(CancelClinicOnboardingCommand command, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdForUserAsync(command.Id, command.UserId, ct);
        if (request == null)
            throw new NotFoundException(nameof(Domain.Entities.ClinicOnboardingRequest), command.Id);

        request.Cancel();
        await requestRepository.SaveChangesAsync(ct);
    }
}
