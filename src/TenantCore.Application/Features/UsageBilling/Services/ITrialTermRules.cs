namespace TenantCore.Application.Features.UsageBilling.Services;

/// <summary>
/// The one place free test days are applied. A package — or a single clinic — may grant 30–90 free
/// days, overriding the Trial plan's own <c>DurationDays</c>.
///
/// Used at the only three points a trial term is created: the admin grant, the clinic's own
/// subscribe, and the onboarding-approval trial.
/// </summary>
public interface ITrialTermRules
{
    /// <summary>
    /// The length of the free trial term, in days. Pass the clinic's override already resolved
    /// against the plan default (<c>clinic.FreeTestDays ?? plan.FreeTestDays</c>) so this stays a
    /// single, testable decision.
    /// </summary>
    int ResolveTrialDurationDays(int? clinicFreeTestDays, int planDurationDays);
}
