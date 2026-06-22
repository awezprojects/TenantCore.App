using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IOpdPaymentApiClient
{
    Task<ApiResponse<OpdPaymentDto>> GetByOpdAsync(Guid opdRegistrationId);
    Task<ApiResponse<Guid>> EnsureAsync(EnsureOpdPaymentRequest request);
    Task<ApiResponse<OpdPaymentDto>> AcceptAsync(Guid id, AcceptOpdPaymentRequest request);
    Task<ApiResponse<OpdPaymentDto>> ApplyDiscountAsync(Guid id, ApplyOpdDiscountRequest request);
    Task<ApiResponse<OpdPaymentDto>> AcceptFullAsync(Guid id, AcceptOpdPaymentFullRequest request);
}
