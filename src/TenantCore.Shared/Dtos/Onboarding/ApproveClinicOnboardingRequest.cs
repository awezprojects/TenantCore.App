namespace TenantCore.Shared.Dtos.Onboarding;

/// <summary>Internal — sent by the Admin portal via the InternalService scheme.</summary>
public record ApproveClinicOnboardingRequest
{
    /// <summary>Required unless GrantTrial is true.</summary>
    public Guid? SubscriptionPlanId { get; init; }
    public bool GrantTrial { get; init; }

    /// <summary>Required for a paid plan. Pre-filled by the Admin portal with the plan's list price; the admin can change it.</summary>
    public decimal? Amount { get; init; }

    /// <summary>Required when Amount differs from the plan's list price.</summary>
    public string? AmountReason { get; init; }

    /// <summary>Optional override of the requester's preferred clinic code.</summary>
    public string? ClinicCode { get; init; }

    public string? ReviewNote { get; init; }

    public Guid AdminUserId { get; init; }
    public string AdminEmail { get; init; } = string.Empty;
}
