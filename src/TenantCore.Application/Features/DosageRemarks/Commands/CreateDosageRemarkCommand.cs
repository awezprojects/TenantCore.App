using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.DosageRemarks.Commands;

public sealed record CreateDosageRemarkCommand(
    Guid ApplicationId,
    MedicineFormType MedicineForm,
    string RemarkEnglish,
    string? RemarkHindi,
    string? RemarkMarathi) : IRequest<DosageRemarkDto>;
