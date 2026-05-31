using Microsoft.AspNetCore.Authorization;

namespace TenantCore.Api.Authorization;

public sealed class ClinicRoleRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Roles { get; } = roles;
}
