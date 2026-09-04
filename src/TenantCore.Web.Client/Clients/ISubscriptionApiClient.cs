using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Web.Client.Clients;

public interface ISubscriptionApiClient
{
    Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetPlansAsync();

    /// <summary>Pass applicationId to check a specific clinic before it is the selected clinic
    /// (e.g. the clinic-landing page listing every clinic the user belongs to). Attaches the
    /// header directly on the request so the shared ClinicContextService doesn't interfere.</summary>
    Task<ApiResponse<SubscriptionStatusDto>> GetStatusAsync(Guid? applicationId = null);
    Task<ApiResponse<IEnumerable<SubscriptionHistoryItemDto>>> GetHistoryAsync();
    Task<ApiResponse<ClinicSubscriptionDto>> SubscribeAsync(SubscribeRequest request);
    Task<ApiResponse> CancelAsync(Guid subscriptionId);
}
