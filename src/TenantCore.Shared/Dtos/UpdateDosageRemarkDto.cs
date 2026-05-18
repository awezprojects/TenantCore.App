using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record UpdateDosageRemarkDto(
    MedicineFormType MedicineForm,
    string RemarkEnglish,
    string? RemarkHindi,
    string? RemarkMarathi,
    bool IsActive);
