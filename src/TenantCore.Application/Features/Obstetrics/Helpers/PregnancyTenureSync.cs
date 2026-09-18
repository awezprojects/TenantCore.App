using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Obstetrics.Helpers;

/// <summary>
/// Keeps the patient's active <see cref="PregnancyTenure"/> in step with the LMP recorded on
/// a prescription.
///
/// A prescription carries its LMP inside the obstetric-data payload, and the LMP may also be
/// pushed through the dedicated SetObstetricLmp endpoint (the form's "Save Now" button, which
/// only renders for an already-saved prescription). Saving the prescription itself never
/// touched the tenure table, so an LMP that only ever travelled with the prescription produced
/// no tenure at all — and the EDD Overdue list (which reads active, past-EDD tenures) stayed
/// permanently empty even for a patient whose EDD had clearly passed.
/// </summary>
public static class PregnancyTenureSync
{
    public static async Task SyncFromLmpAsync(
        IPregnancyTenureRepository tenureRepository,
        Guid patientId,
        Guid applicationId,
        DateOnly? lmp,
        CancellationToken ct = default)
    {
        if (lmp is null || patientId == Guid.Empty || applicationId == Guid.Empty) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eddByLmp = lmp.Value.AddDays(280);

        var existing = await tenureRepository.GetActiveForPatientAsync(patientId, applicationId, ct);
        if (existing is not null)
        {
            // A tenure that has already passed its EDD must be closed before it can be re-dated
            // (same rule as SetObstetricLmpHandler). Skipping instead of throwing keeps saving a
            // prescription possible for such a patient — the block belongs on the LMP action, not
            // on the clinical note.
            if ((existing.EddByUsg ?? existing.EddByLmp) < today) return;

            if (existing.Lmp == lmp.Value && existing.EddByLmp == eddByLmp) return;

            existing.Lmp = lmp.Value;
            existing.EddByLmp = eddByLmp;
            tenureRepository.Update(existing);
            await tenureRepository.SaveChangesAsync(ct);
            return;
        }

        var tenure = new PregnancyTenure
        {
            PatientId = patientId,
            ApplicationId = applicationId,
            Lmp = lmp.Value,
            EddByLmp = eddByLmp,
            Status = PregnancyTenureStatus.Active
        };
        await tenureRepository.AddAsync(tenure, ct);
        await tenureRepository.SaveChangesAsync(ct);
    }
}
