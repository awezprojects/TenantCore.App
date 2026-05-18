using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DosageRemarks.Translators;

public static class DosageRemarkTranslator
{
    public static DosageRemarkDto ToDto(DosageRemark remark) => new()
    {
        Id = remark.Id,
        ApplicationId = remark.ApplicationId,
        MedicineForm = remark.MedicineForm,
        RemarkEnglish = remark.RemarkEnglish,
        RemarkHindi = remark.RemarkHindi,
        RemarkMarathi = remark.RemarkMarathi,
        IsActive = remark.IsActive,
        CreatedAt = remark.CreatedAt
    };
}
