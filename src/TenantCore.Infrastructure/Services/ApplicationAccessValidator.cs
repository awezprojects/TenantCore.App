using Microsoft.AspNetCore.Http;
using TenantCore.Application.Common;

namespace TenantCore.Infrastructure.Services;

public sealed class ApplicationAccessValidator(IHttpContextAccessor httpContextAccessor)
    : IApplicationAccessValidator
{
    public bool CanAccess(Guid applicationId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        var allowedApps = user.FindAll("app_ids")
            .Select(c => Guid.TryParse(c.Value, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToHashSet();

        // No app_ids in token means no clinic linkage — deny
        if (allowedApps.Count == 0) return false;

        return allowedApps.Contains(applicationId);
    }
}
