namespace TenantCore.Application.Features.UsageBilling.Services;

/// <inheritdoc />
public sealed class TrialTermRules : ITrialTermRules
{
    /// <summary>Thirty days is the floor the domain allows, and the safe answer when a plan carries no duration.</summary>
    private const int FallbackDays = 30;

    public int ResolveTrialDurationDays(int? clinicFreeTestDays, int planDurationDays)
    {
        // A granted free-test-days value always wins: that is the whole point of the field.
        if (clinicFreeTestDays is { } granted)
            return granted;

        return planDurationDays > 0 ? planDurationDays : FallbackDays;
    }
}
