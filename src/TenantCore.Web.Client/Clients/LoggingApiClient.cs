using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class LoggingApiClient(HttpClient httpClient, ClinicContextService clinicContext) : ILoggingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogErrorAsync(FrontendErrorLogRequest request)
    {
        try
        {
            var applicationId = clinicContext.SelectedApplicationId == Guid.Empty
                ? request.ApplicationId
                : clinicContext.SelectedApplicationId;

            await httpClient.PostAsJsonAsync(
                "api/logs/frontend-error",
                request with { ApplicationId = applicationId },
                JsonOptions);
        }
        catch
        {
            // Logging must never crash the app it's instrumenting.
        }
    }
}
