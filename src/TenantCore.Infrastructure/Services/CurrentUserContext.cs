using Microsoft.AspNetCore.Http;
using TenantCore.Application.Common;

namespace TenantCore.Infrastructure.Services;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst("nameid")
                     ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                     ?? user?.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
