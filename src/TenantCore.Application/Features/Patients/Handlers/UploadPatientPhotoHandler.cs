using MediatR;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.Patients.Handlers;

public sealed class UploadPatientPhotoHandler(
    IPatientRepository repository,
    IBlobStorageService blobStorage)
    : IRequestHandler<UploadPatientPhotoCommand, string>
{
    public async Task<string> Handle(UploadPatientPhotoCommand request, CancellationToken cancellationToken)
    {
        var patient = await repository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        if (patient.ApplicationId != request.ApplicationId)
            throw new UnauthorizedAccessException("Access denied.");

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var blobPath = $"clinics/{request.ApplicationName}/patients/{request.PatientId}/profile/photo{ext}";

        var url = await blobStorage.UploadAsync(request.PhotoStream, blobPath, request.ContentType, cancellationToken);

        patient.UpdatePhotoUrl(url);
        repository.Update(patient);
        await repository.SaveChangesAsync(cancellationToken);

        return url;
    }
}
