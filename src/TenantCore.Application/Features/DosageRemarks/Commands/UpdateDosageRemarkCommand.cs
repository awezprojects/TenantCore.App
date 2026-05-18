using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.DosageRemarks.Commands;

public sealed record UpdateDosageRemarkCommand(
    Guid Id,
    Guid ApplicationId,
    MedicineFormType MedicineForm,
    string RemarkEnglish,
    string? RemarkHindi,
    string? RemarkMarathi,
    bool IsActive) : IRequest<DosageRemarkDto>;
