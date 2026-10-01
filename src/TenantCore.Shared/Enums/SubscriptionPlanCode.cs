namespace TenantCore.Shared.Enums;

public enum SubscriptionPlanCode
{
    Trial = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4,

    /// <summary>
    /// Any admin-created package. Unlike the four seeded codes this one is NOT unique —
    /// SubscriptionPlans has a filtered unique index on Code that excludes it, so the
    /// GetByCodeAsync(Trial) lookup stays unambiguous while any number of Custom plans exist.
    /// </summary>
    Custom = 5
}
