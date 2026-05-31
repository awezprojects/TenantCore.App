using MediatR;

namespace TenantCore.Application.Features.Patients.Commands;

public sealed record UploadPatientPhotoCommand(
    Guid PatientId,
    Guid ApplicationId,
    string ApplicationName,
    Stream PhotoStream,
    string FileName,
    string ContentType) : IRequest<string>;
