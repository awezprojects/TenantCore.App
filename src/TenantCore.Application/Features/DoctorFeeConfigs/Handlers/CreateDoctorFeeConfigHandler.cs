using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class CreateDoctorFeeConfigHandler(IDoctorFeeConfigRepository repository) : IRequestHandler<CreateDoctorFeeConfigCommand, Guid>
{
    public async Task<Guid> Handle(CreateDoctorFeeConfigCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByDoctorProfileIdAsync(request.Request.DoctorProfileId, request.ApplicationId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("A fee config already exists for this doctor.");

        var config = DoctorFeeConfigTranslator.ToEntity(request);
        await repository.AddAsync(config, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return config.Id;
    }
}
