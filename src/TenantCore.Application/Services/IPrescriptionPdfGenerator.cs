using TenantCore.Domain.Entities;

namespace TenantCore.Application.Services;

/// <summary>
/// Renders a complete prescription (header, appointment details, medicines, footer)
/// to a PDF document used as the email attachment.
/// </summary>
public interface IPrescriptionPdfGenerator
{
    Task<byte[]> GenerateAsync(
        Prescription prescription,
        Patient patient,
        OpdRegistration opdRegistration,
        CancellationToken ct = default);
}
