using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Translators;

public static class ObstetricDatesTranslator
{
    public static ObstetricDatesDto ToDto(ObstetricPrescriptionData data) => new()
    {
        Id             = data.Id,
        PrescriptionId = data.PrescriptionId,
        Lmp            = data.Lmp,
        EddByLmp       = data.EddByLmp,
        EddByUsg       = data.EddByUsg,
    };
}
