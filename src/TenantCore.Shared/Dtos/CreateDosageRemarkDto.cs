using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public sealed record CreateDosageRemarkDto(
    MedicineFormType MedicineForm,
    string RemarkEnglish,
    string? RemarkHindi,
    string? RemarkMarathi);
