using TenantCore.Shared.Dtos;

namespace TenantCore.Web.Client.Clients;

public interface ILoggingApiClient
{
    /// <summary>Best-effort — never throws. Logging must never surface an error to the user or block the caller.</summary>
    Task LogErrorAsync(FrontendErrorLogRequest request);
}
