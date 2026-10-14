using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// The clinic's own pay-as-you-go surface: wallet, metered usage and the registration fee.
///
/// Nothing here is fetched on a tab, sort or filter — the usage screen calls
/// <see cref="GetUsageAsync"/> once per range and serves everything else from memory (workspace rule 7).
/// </summary>
public interface IUsageBillingApiClient
{
    Task<ApiResponse<ClinicBillingSummaryDto>> GetSummaryAsync();
    Task<ApiResponse<ClinicWalletDto>> GetWalletAsync();

    /// <summary>One call per range; the result is capped server-side (see <c>IsCapped</c>).</summary>
    Task<ApiResponse<ClinicUsageDto>> GetUsageAsync(DateTime from, DateTime to);

    Task<ApiResponse<WalletTopUpPaymentDto>> CreateTopUpAsync(decimal amount);

    /// <summary>"I've paid — check now". Throttled server-side to once per 30 seconds per payment.</summary>
    Task<ApiResponse> CheckTopUpAsync(Guid paymentId);

    /// <summary>Returns the most recent registration fee payment (open or expired), or null when none exists yet.</summary>
    Task<ApiResponse<RegistrationFeePaymentDto?>> GetActiveRegistrationFeePaymentAsync();

    Task<ApiResponse<RegistrationFeePaymentDto>> CreateRegistrationFeePaymentAsync();

    /// <summary>"I've paid — check now" for the one-time registration fee.</summary>
    Task<ApiResponse> CheckRegistrationFeePaymentAsync(Guid paymentId);

    /// <summary>
    /// Formats the already-loaded <paramref name="usage"/> as an Excel workbook server-side and returns
    /// the raw bytes. The caller must not re-fetch from <see cref="GetUsageAsync"/> just to export
    /// (workspace rule 7) — pass the cached dto.
    /// </summary>
    Task<ApiResponse<byte[]>> ExportExcelAsync(ClinicUsageDto usage, string currency);
}
