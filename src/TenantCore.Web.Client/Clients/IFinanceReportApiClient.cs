using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

public interface IFinanceReportApiClient
{
    Task<ApiResponse<FinanceDashboardSummaryDto>> GetDashboardAsync(DateTime? date = null);
    Task<ApiResponse<DailyCollectionReportDto>> GetDailyAsync(DateTime? date = null);
    Task<ApiResponse<WeeklyCollectionReportDto>> GetWeeklyAsync(DateTime? weekStartDate = null);
    Task<ApiResponse<MonthlyCollectionReportDto>> GetMonthlyAsync(int? year = null, int? month = null);
    Task<ApiResponse<DateRangeCollectionReportDto>> GetDateRangeAsync(DateTime from, DateTime to);
    Task<ApiResponse<IEnumerable<ReceptionWiseReportDto>>> GetReceptionWiseAsync(DateTime from, DateTime to);
    Task<ApiResponse<ExpenseSummaryReportDto>> GetExpenseSummaryAsync(DateTime from, DateTime to);
    Task<ApiResponse<DoctorWiseCollectionDto>> GetDoctorWiseAsync(DateTime from, DateTime to, Guid? doctorUserId = null);
}
