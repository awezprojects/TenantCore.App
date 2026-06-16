using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.PregnancyTenures.Translators;

public static class PregnancyTenureTranslator
{
    public static PregnancyTenureDto ToDto(PregnancyTenure tenure) => new()
    {
        Id = tenure.Id,
        PatientId = tenure.PatientId,
        ApplicationId = tenure.ApplicationId,
        Lmp = tenure.Lmp,
        EddByLmp = tenure.EddByLmp,
        EddByUsg = tenure.EddByUsg,
        EffectiveEdd = tenure.EddByUsg ?? tenure.EddByLmp,
        Status = tenure.Status,
        Outcome = tenure.Outcome,
        ClosedAt = tenure.ClosedAt,
        Notes = tenure.Notes,
        CreatedAt = tenure.CreatedAt
    };

    public static PregnancyTenureSummaryDto ToSummaryDto(PregnancyTenure tenure)
    {
        var effectiveEdd = tenure.EddByUsg ?? tenure.EddByLmp;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysOverdue = Math.Max(0, today.DayNumber - effectiveEdd.DayNumber);
        return new PregnancyTenureSummaryDto
        {
            TenureId = tenure.Id,
            PatientId = tenure.PatientId,
            PatientPhone = tenure.Patient.PhoneNumber,
            PatientFullName = $"{tenure.Patient.FirstName} {tenure.Patient.LastName}".Trim(),
            Lmp = tenure.Lmp,
            EddByLmp = tenure.EddByLmp,
            EddByUsg = tenure.EddByUsg,
            EffectiveEdd = effectiveEdd,
            Status = tenure.Status,
            DaysOverdue = daysOverdue
        };
    }
}
