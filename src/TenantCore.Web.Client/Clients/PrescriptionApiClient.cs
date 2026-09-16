using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Enums;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class PrescriptionApiClient(HttpClient httpClient, AuthStateService authState) : IPrescriptionApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private void SetAuth() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(authState.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", authState.AccessToken);

    private static async Task<ApiResponse<T>> Ok<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var (message, fieldErrors) = await ExtractUserMessageAndFields(response);
            return new ApiResponse<T> { Success = false, Message = message, Errors = fieldErrors };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    // Parses the ProblemDetails JSON returned by the API and extracts the
    // user-facing "detail" field (plus per-field validation errors) so the
    // raw JSON / GUIDs / routes never reach the UI. Mirrors ClinicApiClient.
    private static async Task<(string Message, List<string> FieldErrors)> ExtractUserMessageAndFields(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    var text = root.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return (text, []);
                }

                var fieldErrors = new List<string>();
                if (root.TryGetProperty("errors", out var errors))
                {
                    foreach (var field in errors.EnumerateObject())
                        foreach (var msg in field.Value.EnumerateArray())
                        {
                            var s = msg.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) fieldErrors.Add($"{field.Name}: {s}");
                        }
                }

                if (root.TryGetProperty("detail", out var detail))
                {
                    var text = detail.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return (text, fieldErrors);
                }

                if (fieldErrors.Count > 0)
                    return (string.Join(" ", fieldErrors.Select(e => e[(e.IndexOf(':') + 2)..])), fieldErrors);
            }
        }
        catch { /* fall through to default */ }

        var fallback = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest           => "Please check your input and try again.",
            System.Net.HttpStatusCode.Unauthorized         => "Your session has expired. Please log in again.",
            System.Net.HttpStatusCode.Forbidden             => "You don't have permission to perform this action.",
            System.Net.HttpStatusCode.NotFound              => "The requested record was not found.",
            System.Net.HttpStatusCode.Conflict              => "This action conflicts with existing data.",
            System.Net.HttpStatusCode.InternalServerError   => "A server error occurred. Please try again.",
            _                                                => "Something went wrong. Please try again."
        };
        return (fallback, []);
    }

    private static ApiResponse<T> Fail<T>(string message) =>
        new() { Success = false, Message = message, Errors = [message] };

    public async Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
        int page = 1, int pageSize = 20, string? search = null,
        Guid? doctorUserId = null, Guid? patientId = null,
        DateTime? from = null, DateTime? to = null,
        Guid? applicationId = null)
    {
        try
        {
            SetAuth();
            var url = $"api/prescriptions?page={page}&pageSize={pageSize}";
            if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";
            if (doctorUserId.HasValue) url += $"&doctorUserId={doctorUserId}";
            if (patientId.HasValue) url += $"&patientId={patientId}";
            if (from.HasValue) url += $"&from={from.Value:yyyy-MM-ddTHH:mm:ss}";
            if (to.HasValue) url += $"&to={to.Value:yyyy-MM-ddTHH:mm:ss}";

            if (applicationId.HasValue)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("X-Application-Id", applicationId.Value.ToString());
                return await Ok<PagedResult<PrescriptionDto>>(await httpClient.SendAsync(req));
            }

            return await Ok<PagedResult<PrescriptionDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<PrescriptionDto>>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid id)
    {
        try { SetAuth(); return await Ok<PrescriptionDto>(await httpClient.GetAsync($"api/prescriptions/{id}")); }
        catch (Exception ex) { return Fail<PrescriptionDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionDto>> GetPrescriptionByOpdIdAsync(Guid opdRegistrationId)
    {
        try { SetAuth(); return await Ok<PrescriptionDto>(await httpClient.GetAsync($"api/prescriptions/opd/{opdRegistrationId}")); }
        catch (Exception ex) { return Fail<PrescriptionDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto)
    {
        try { SetAuth(); return await Ok<PrescriptionDto>(await httpClient.PostAsJsonAsync("api/prescriptions", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<PrescriptionDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionDto dto)
    {
        try { SetAuth(); return await Ok<PrescriptionDto>(await httpClient.PutAsJsonAsync($"api/prescriptions/{id}", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<PrescriptionDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionDto>> SubmitPrescriptionAsync(Guid id)
    {
        try { SetAuth(); return await Ok<PrescriptionDto>(await httpClient.PostAsync($"api/prescriptions/{id}/submit", null)); }
        catch (Exception ex) { return Fail<PrescriptionDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionReportDto>> UploadReportAsync(Guid prescriptionId, Stream fileStream, string fileName)
    {
        try
        {
            SetAuth();
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            return await Ok<PrescriptionReportDto>(await httpClient.PostAsync($"api/prescriptions/{prescriptionId}/reports", content));
        }
        catch (Exception ex) { return Fail<PrescriptionReportDto>(ex.Message); }
    }

    public async Task<ApiResponse<PagedResult<DosageRemarkDto>>> GetDosageRemarksAsync(
        int page = 1, int pageSize = 50, MedicineFormType? form = null)
    {
        try
        {
            SetAuth();
            var url = $"api/dosage-remarks?page={page}&pageSize={pageSize}";
            if (form.HasValue) url += $"&form={(int)form.Value}";
            return await Ok<PagedResult<DosageRemarkDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<DosageRemarkDto>>(ex.Message); }
    }

    public async Task<ApiResponse<DosageRemarkDto>> CreateDosageRemarkAsync(CreateDosageRemarkDto dto)
    {
        try { SetAuth(); return await Ok<DosageRemarkDto>(await httpClient.PostAsJsonAsync("api/dosage-remarks", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<DosageRemarkDto>(ex.Message); }
    }

    public async Task<ApiResponse<DosageRemarkDto>> UpdateDosageRemarkAsync(Guid id, UpdateDosageRemarkDto dto)
    {
        try { SetAuth(); return await Ok<DosageRemarkDto>(await httpClient.PutAsJsonAsync($"api/dosage-remarks/{id}", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<DosageRemarkDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteDosageRemarkAsync(Guid id)
    {
        try
        {
            SetAuth();
            var response = await httpClient.DeleteAsync($"api/dosage-remarks/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse<bool> { Success = true, Data = true }
                : new ApiResponse<bool> { Success = false, Message = await response.Content.ReadAsStringAsync() };
        }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionConfigDto>> GetPrescriptionConfigAsync()
    {
        try { SetAuth(); return await Ok<PrescriptionConfigDto>(await httpClient.GetAsync("api/prescription-config")); }
        catch (Exception ex) { return Fail<PrescriptionConfigDto>(ex.Message); }
    }

    public async Task<ApiResponse<PrescriptionConfigDto>> UpdatePrescriptionConfigAsync(UpdatePrescriptionConfigDto dto)
    {
        try { SetAuth(); return await Ok<PrescriptionConfigDto>(await httpClient.PutAsJsonAsync("api/prescription-config", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<PrescriptionConfigDto>(ex.Message); }
    }
}
