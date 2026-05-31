using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.DoctorProfiles.Commands;
using TenantCore.Application.Features.DoctorProfiles.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorProfiles.Handlers;

public sealed class UpsertDoctorProfileHandler(
    IDoctorProfileRepository repository,
    ILogger<UpsertDoctorProfileHandler> logger)
    : IRequestHandler<UpsertDoctorProfileCommand, DoctorProfileDto>
{
    public async Task<DoctorProfileDto> Handle(UpsertDoctorProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existing is null)
        {
            logger.LogInformation("Creating doctor profile for user {UserId}", request.UserId);
            var profile = DoctorProfile.Create(
                request.UserId,
                request.RegistrationNumber,
                request.Specialty,
                request.QualificationDetails);

            await repository.AddAsync(profile, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return DoctorProfileTranslator.ToDto(profile);
        }

        logger.LogInformation("Updating doctor profile for user {UserId}", request.UserId);
        existing.Update(
            request.RegistrationNumber,
            request.Specialty,
            request.QualificationDetails);

        repository.Update(existing);
        await repository.SaveChangesAsync(cancellationToken);
        return DoctorProfileTranslator.ToDto(existing);
    }
}
