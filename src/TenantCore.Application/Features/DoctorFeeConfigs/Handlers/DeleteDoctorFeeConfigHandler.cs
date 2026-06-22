using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class DeleteDoctorFeeConfigHandler(IDoctorFeeConfigRepository repository) : IRequestHandler<DeleteDoctorFeeConfigCommand>
{
    public async Task Handle(DeleteDoctorFeeConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (config is null || config.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(DoctorFeeConfig), request.Id);

        repository.Delete(config);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
