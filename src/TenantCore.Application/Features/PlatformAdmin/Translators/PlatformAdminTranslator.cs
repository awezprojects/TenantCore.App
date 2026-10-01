using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Features.PlatformAdmin.Translators;

public static class PlatformAdminTranslator
{
    /// <summary>
    /// Request DTO → domain contact. Name falls back to the clinic name when the portal could not
    /// find a person's name in Auth: an empty RecipientName would make the notification consumer
    /// dead-letter the email without ever attempting to send it (workspace rule 9).
    /// </summary>
    public static ClinicContact ToContact(ClinicContactRequest request)
    {
        var clinicName = request.ClinicName.Trim();
        var name = string.IsNullOrWhiteSpace(request.ContactName) ? clinicName : request.ContactName.Trim();
        var phone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();

        return new ClinicContact(clinicName, name, request.ContactEmail.Trim(), phone);
    }

    public static PlanDetails ToPlanDetails(SaveSubscriptionPlanRequest request) => new(
        request.Name.Trim(),
        request.Description?.Trim() ?? string.Empty,
        request.DurationDays,
        request.Price,
        request.IsPopular,
        request.IsPublic,
        request.DisplayOrder);

    /// <summary>Null for a blank reason, so an "unchanged amount" link stores no reason at all.</summary>
    public static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
