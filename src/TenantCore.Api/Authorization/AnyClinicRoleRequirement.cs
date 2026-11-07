using Microsoft.AspNetCore.Authorization;

namespace TenantCore.Api.Authorization;

// DEF-MR-11: ClinicRoleRequirement checks the role for one specific clinic (route id, or the
// X-Application-Id-derived "currently selected" clinic). This checks the SAME role set but
// across every clinic on the user's app_roles claim — for actions on the user's own profile
// that aren't scoped to any one clinic, so there's no clinic id to check against at all.
public sealed class AnyClinicRoleRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Roles { get; } = roles;
}
