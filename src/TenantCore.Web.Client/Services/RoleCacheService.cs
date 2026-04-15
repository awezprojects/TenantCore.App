using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Services;

public class RoleCacheService
{
    private readonly Dictionary<Guid, List<RoleResponseDto>> _roleCache = new();

    public List<RoleResponseDto>? GetRoles(Guid applicationId)
    {
        if (_roleCache.TryGetValue(applicationId, out var roles))
            return roles;
        return null;
    }

    public void SetRoles(Guid applicationId, List<RoleResponseDto> roles)
    {
        _roleCache[applicationId] = roles;
    }
}
