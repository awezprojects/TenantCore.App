namespace TenantCore.Application.Common;

/// <summary>
/// Resolves the authenticated user for the current request, without any Application-layer
/// code referencing <c>HttpContext</c> directly (see ADR-003 — ASP.NET Core types never
/// appear in Application). Returns null outside an HTTP request (e.g. no user resolvable).
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }
}
