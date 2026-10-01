namespace TenantCore.Application.Features.PlatformAdmin.Models;

/// <summary>The editable fields of a catalogue plan — shared by create and update. Code is never settable.</summary>
public sealed record PlanDetails(
    string Name,
    string Description,
    int DurationDays,
    decimal Price,
    bool IsPopular,
    bool IsPublic,
    int DisplayOrder);
