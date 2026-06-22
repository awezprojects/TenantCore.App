using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class FinanceReportApiClient(HttpClient httpClient, AuthStateService authState) : IFinanceReportApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static async Task<ApiResponse<T>> Read<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T> { Success = false, Message = err };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    public async Task<ApiResponse<FinanceDashboardSummaryDto>> GetDashboardAsync(DateTime? date = null)
    {
        try
        {
            SetAuth();
            var url = date.HasValue ? $"api/finance-reports/dashboard?date={date.Value:O}" : "api/finance-reports/dashboard";
            return await Read<FinanceDashboardSummaryDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<FinanceDashboardSummaryDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DailyCollectionReportDto>> GetDailyAsync(DateTime? date = null)
    {
        try
        {
            SetAuth();
            var url = date.HasValue ? $"api/finance-reports/daily?date={date.Value:O}" : "api/finance-reports/daily";
            return await Read<DailyCollectionReportDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<DailyCollectionReportDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<WeeklyCollectionReportDto>> GetWeeklyAsync(DateTime? weekStartDate = null)
    {
        try
        {
            SetAuth();
            var url = weekStartDate.HasValue ? $"api/finance-reports/weekly?weekStartDate={weekStartDate.Value:O}" : "api/finance-reports/weekly";
            return await Read<WeeklyCollectionReportDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<WeeklyCollectionReportDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<MonthlyCollectionReportDto>> GetMonthlyAsync(int? year = null, int? month = null)
    {
        try
        {
            SetAuth();
            var qs = new List<string>();
            if (year.HasValue) qs.Add($"year={year}");
            if (month.HasValue) qs.Add($"month={month}");
            var url = qs.Count > 0 ? "api/finance-reports/monthly?" + string.Join("&", qs) : "api/finance-reports/monthly";
            return await Read<MonthlyCollectionReportDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<MonthlyCollectionReportDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DateRangeCollectionReportDto>> GetDateRangeAsync(DateTime from, DateTime to)
    {
        try
        {
            SetAuth();
            var utcOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
            return await Read<DateRangeCollectionReportDto>(await httpClient.GetAsync(
                $"api/finance-reports/date-range?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&utcOffset={utcOffset}"));
        }
        catch (Exception ex) { return new ApiResponse<DateRangeCollectionReportDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<IEnumerable<ReceptionWiseReportDto>>> GetReceptionWiseAsync(DateTime from, DateTime to)
    {
        try
        {
            SetAuth();
            return await Read<IEnumerable<ReceptionWiseReportDto>>(await httpClient.GetAsync($"api/finance-reports/reception-wise?from={from:O}&to={to:O}"));
        }
        catch (Exception ex) { return new ApiResponse<IEnumerable<ReceptionWiseReportDto>> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<ExpenseSummaryReportDto>> GetExpenseSummaryAsync(DateTime from, DateTime to)
    {
        try
        {
            SetAuth();
            var utcOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
            return await Read<ExpenseSummaryReportDto>(await httpClient.GetAsync(
                $"api/finance-reports/expenses?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&utcOffset={utcOffset}"));
        }
        catch (Exception ex) { return new ApiResponse<ExpenseSummaryReportDto> { Success = false, Message = ex.Message }; }
    }

    public async Task<ApiResponse<DoctorWiseCollectionDto>> GetDoctorWiseAsync(DateTime from, DateTime to, Guid? doctorUserId = null)
    {
        try
        {
            SetAuth();
            var utcOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
            var url = $"api/finance-reports/doctor-wise?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&utcOffset={utcOffset}";
            if (doctorUserId.HasValue) url += $"&doctorUserId={doctorUserId.Value}";
            return await Read<DoctorWiseCollectionDto>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return new ApiResponse<DoctorWiseCollectionDto> { Success = false, Message = ex.Message }; }
    }
}
