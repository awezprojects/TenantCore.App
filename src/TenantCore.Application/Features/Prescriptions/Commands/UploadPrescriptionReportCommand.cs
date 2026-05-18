using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Prescriptions.Commands;

public sealed record UploadPrescriptionReportCommand(
    Guid PrescriptionId,
    Guid ApplicationId,
    string FileName,
    byte[] FileBytes,
    string FileExtension) : IRequest<PrescriptionReportDto>;
