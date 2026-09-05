using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Web.Client.Services;

namespace TenantCore.Web.Client.Clients;

public class ClinicApiClient(HttpClient httpClient, AuthStateService authState) : IClinicApiClient
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
            var message = await ExtractUserMessage(response);
            return new ApiResponse<T> { Success = false, Message = message };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    // Parses the ProblemDetails JSON returned by the API and extracts the
    // user-facing "detail" field. Falls back to a status-code-based default
    // so the raw JSON / GUIDs / routes never reach the UI.
    private static async Task<string> ExtractUserMessage(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("detail", out var detail))
                {
                    var text = detail.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }

                // FluentValidation errors come as extensions["errors"]
                if (root.TryGetProperty("errors", out var errors))
                {
                    var messages = new List<string>();
                    foreach (var field in errors.EnumerateObject())
                        foreach (var msg in field.Value.EnumerateArray())
                        {
                            var s = msg.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) messages.Add(s);
                        }
                    if (messages.Count > 0) return string.Join(" ", messages);
                }
            }
        }
        catch { /* fall through to default */ }

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest           => "Please check your input and try again.",
            System.Net.HttpStatusCode.Unauthorized         => "Your session has expired. Please log in again.",
            System.Net.HttpStatusCode.Forbidden            => "You don't have permission to perform this action.",
            System.Net.HttpStatusCode.NotFound             => "The requested record was not found.",
            System.Net.HttpStatusCode.Conflict             => "This action conflicts with existing data.",
            System.Net.HttpStatusCode.InternalServerError  => "A server error occurred. Please try again.",
            _                                              => "Something went wrong. Please try again."
        };
    }

    private static ApiResponse<T> Fail<T>(string _) =>
        new() { Success = false, Message = "Could not connect to the server. Please check your connection and try again." };

    // --- Patients ---

    public async Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        try
        {
            SetAuth();
            var url = $"api/patients?page={page}&pageSize={pageSize}" +
                      (search is not null ? $"&search={Uri.EscapeDataString(search)}" : "");
            return await Ok<PagedResult<PatientDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<PatientDto>>(ex.Message); }
    }

    public async Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id)
    {
        try
        {
            SetAuth();
            return await Ok<PatientDto>(await httpClient.GetAsync($"api/patients/{id}"));
        }
        catch (Exception ex) { return Fail<PatientDto>(ex.Message); }
    }

    public async Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<PatientDto>(await httpClient.PostAsJsonAsync("api/patients", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<PatientDto>(ex.Message); }
    }

    public async Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<PatientDto>(await httpClient.PutAsJsonAsync($"api/patients/{id}", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<PatientDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeletePatientAsync(Guid id)
    {
        try
        {
            SetAuth();
            var response = await httpClient.DeleteAsync($"api/patients/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse<bool> { Success = true, Data = true }
                : new ApiResponse<bool> { Success = false, Message = await response.Content.ReadAsStringAsync() };
        }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }

    public async Task<ApiResponse<string>> UploadPatientPhotoAsync(Guid id, IBrowserFile file)
    {
        try
        {
            SetAuth();
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "photo", file.Name);
            var response = await httpClient.PostAsync($"api/patients/{id}/upload-photo", content);
            if (!response.IsSuccessStatusCode)
                return new ApiResponse<string> { Success = false, Message = await response.Content.ReadAsStringAsync() };
            var result = await response.Content.ReadFromJsonAsync<PhotoUploadResult>(JsonOptions);
            return new ApiResponse<string> { Success = true, Data = result?.Url };
        }
        catch (Exception ex) { return Fail<string>(ex.Message); }
    }

    private sealed record PhotoUploadResult(string Url);

    // --- OPD Registrations ---

    public async Task<ApiResponse<PagedResult<OpdRegistrationDto>>> GetOpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null, Guid? doctorUserId = null, bool todayOnly = false, DateTime? fromDate = null, DateTime? toDate = null, TenantCore.Shared.Enums.OpdStatus? status = null, bool notVisited = false, Guid? applicationId = null)
    {
        try
        {
            SetAuth();
            var url = $"api/opd-registrations?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (doctorUserId.HasValue) url += $"&doctorUserId={doctorUserId}";
            if (todayOnly)         url += "&todayOnly=true";
            if (fromDate.HasValue) url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            if (toDate.HasValue)   url += $"&toDate={toDate.Value:yyyy-MM-dd}";
            if (status.HasValue)   url += $"&status={(int)status.Value}";
            if (notVisited)        url += "&notVisited=true";

            // When an explicit applicationId is supplied (e.g. landing-page per-clinic stats),
            // attach it directly on the request so the shared ClinicContextService (Guid.Empty
            // at that point) doesn't interfere via the delegating handler.
            if (applicationId.HasValue)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("X-Application-Id", applicationId.Value.ToString());
                return await Ok<PagedResult<OpdRegistrationDto>>(await httpClient.SendAsync(req));
            }

            return await Ok<PagedResult<OpdRegistrationDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<OpdRegistrationDto>>(ex.Message); }
    }

    public async Task<ApiResponse<OpdRegistrationDto>> GetOpdRegistrationByIdAsync(Guid id)
    {
        try
        {
            SetAuth();
            return await Ok<OpdRegistrationDto>(await httpClient.GetAsync($"api/opd-registrations/{id}"));
        }
        catch (Exception ex) { return Fail<OpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<OpdRegistrationDto>> CreateOpdRegistrationAsync(CreateOpdRegistrationDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<OpdRegistrationDto>(await httpClient.PostAsJsonAsync("api/opd-registrations", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<OpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<OpdRegistrationDto>> UpdateOpdRegistrationAsync(Guid id, UpdateOpdRegistrationDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<OpdRegistrationDto>(await httpClient.PutAsJsonAsync($"api/opd-registrations/{id}", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<OpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteOpdRegistrationAsync(Guid id)
    {
        try
        {
            SetAuth();
            var response = await httpClient.DeleteAsync($"api/opd-registrations/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse<bool> { Success = true, Data = true }
                : new ApiResponse<bool> { Success = false, Message = await ExtractUserMessage(response) };
        }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }

    // --- IPD Registrations ---

    public async Task<ApiResponse<PagedResult<IpdRegistrationDto>>> GetIpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        try
        {
            SetAuth();
            var url = $"api/ipd-registrations?page={page}&pageSize={pageSize}" +
                      (search is not null ? $"&search={Uri.EscapeDataString(search)}" : "");
            return await Ok<PagedResult<IpdRegistrationDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<PagedResult<IpdRegistrationDto>>(ex.Message); }
    }

    public async Task<ApiResponse<IpdRegistrationDto>> GetIpdRegistrationByIdAsync(Guid id)
    {
        try
        {
            SetAuth();
            return await Ok<IpdRegistrationDto>(await httpClient.GetAsync($"api/ipd-registrations/{id}"));
        }
        catch (Exception ex) { return Fail<IpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<IpdRegistrationDto>> CreateIpdRegistrationAsync(CreateIpdRegistrationDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<IpdRegistrationDto>(await httpClient.PostAsJsonAsync("api/ipd-registrations", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<IpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<IpdRegistrationDto>> UpdateIpdRegistrationAsync(Guid id, UpdateIpdRegistrationDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<IpdRegistrationDto>(await httpClient.PutAsJsonAsync($"api/ipd-registrations/{id}", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<IpdRegistrationDto>(ex.Message); }
    }

    public async Task<ApiResponse<IpdRegistrationDto>> DischargePatientAsync(Guid id, DischargePatientDto dto)
    {
        try
        {
            SetAuth();
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/ipd-registrations/{id}/discharge")
            {
                Content = JsonContent.Create(dto, options: JsonOptions)
            };
            return await Ok<IpdRegistrationDto>(await httpClient.SendAsync(request));
        }
        catch (Exception ex) { return Fail<IpdRegistrationDto>(ex.Message); }
    }

    // --- Clinic Settings ---

    public async Task<ApiResponse<ClinicFeeConfigDto>> GetFeesAsync()
    {
        try
        {
            SetAuth();
            return await Ok<ClinicFeeConfigDto>(await httpClient.GetAsync("api/clinic-settings/fees"));
        }
        catch (Exception ex) { return Fail<ClinicFeeConfigDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicFeeConfigDto>> UpdateFeesAsync(UpdateClinicFeeConfigDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<ClinicFeeConfigDto>(await httpClient.PutAsJsonAsync("api/clinic-settings/fees", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<ClinicFeeConfigDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicFeatureFlagsDto>> GetFeatureFlagsAsync()
    {
        try
        {
            SetAuth();
            return await Ok<ClinicFeatureFlagsDto>(await httpClient.GetAsync("api/clinic-settings/feature-flags"));
        }
        catch (Exception ex) { return Fail<ClinicFeatureFlagsDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicFeatureFlagsDto>> UpdateFeatureFlagsAsync(UpdateClinicFeatureFlagsDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<ClinicFeatureFlagsDto>(await httpClient.PutAsJsonAsync("api/clinic-settings/feature-flags", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<ClinicFeatureFlagsDto>(ex.Message); }
    }

    public async Task<ApiResponse<IEnumerable<StateDto>>> GetStatesAsync()
    {
        try
        {
            SetAuth();
            return await Ok<IEnumerable<StateDto>>(await httpClient.GetAsync("api/clinic-settings/states"));
        }
        catch (Exception ex) { return Fail<IEnumerable<StateDto>>(ex.Message); }
    }

    public async Task<ApiResponse<IEnumerable<CityDto>>> GetCitiesByStateAsync(Guid stateId)
    {
        try
        {
            SetAuth();
            return await Ok<IEnumerable<CityDto>>(await httpClient.GetAsync($"api/clinic-settings/states/{stateId}/cities"));
        }
        catch (Exception ex) { return Fail<IEnumerable<CityDto>>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicLocationDto>> GetClinicLocationAsync()
    {
        try
        {
            SetAuth();
            return await Ok<ClinicLocationDto>(await httpClient.GetAsync("api/clinic-settings/location"));
        }
        catch (Exception ex) { return Fail<ClinicLocationDto>(ex.Message); }
    }

    public async Task<ApiResponse<ClinicLocationDto>> UpsertClinicLocationAsync(UpsertClinicLocationDto dto)
    {
        try
        {
            SetAuth();
            return await Ok<ClinicLocationDto>(await httpClient.PutAsJsonAsync("api/clinic-settings/location", dto, JsonOptions));
        }
        catch (Exception ex) { return Fail<ClinicLocationDto>(ex.Message); }
    }

    public async Task<ApiResponse<IEnumerable<DoctorDto>>> GetDoctorsAsync()
    {
        try
        {
            SetAuth();
            return await Ok<IEnumerable<DoctorDto>>(await httpClient.GetAsync("api/clinic-settings/doctors"));
        }
        catch (Exception ex) { return Fail<IEnumerable<DoctorDto>>(ex.Message); }
    }

    public async Task<ApiResponse<int>> GetDoctorOpdCountAsync(Guid doctorUserId, CancellationToken ct = default)
    {
        try
        {
            SetAuth();
            return await Ok<int>(await httpClient.GetAsync($"api/opd-registrations/doctor-count?doctorUserId={doctorUserId}", ct));
        }
        catch (Exception ex) { return Fail<int>(ex.Message); }
    }

    // --- Wards ---

    public async Task<ApiResponse<IEnumerable<WardDto>>> GetWardsAsync()
    {
        try { SetAuth(); return await Ok<IEnumerable<WardDto>>(await httpClient.GetAsync("api/wards")); }
        catch (Exception ex) { return Fail<IEnumerable<WardDto>>(ex.Message); }
    }

    public async Task<ApiResponse<WardDto>> CreateWardAsync(CreateWardDto dto)
    {
        try { SetAuth(); return await Ok<WardDto>(await httpClient.PostAsJsonAsync("api/wards", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<WardDto>(ex.Message); }
    }

    public async Task<ApiResponse<WardDto>> UpdateWardAsync(Guid id, UpdateWardDto dto)
    {
        try { SetAuth(); return await Ok<WardDto>(await httpClient.PutAsJsonAsync($"api/wards/{id}", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<WardDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteWardAsync(Guid id)
    {
        try { SetAuth(); var r = await httpClient.DeleteAsync($"api/wards/{id}"); return new ApiResponse<bool> { Success = r.IsSuccessStatusCode, Data = r.IsSuccessStatusCode }; }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }

    // --- Rooms ---

    public async Task<ApiResponse<IEnumerable<RoomDto>>> GetRoomsByWardAsync(Guid wardId)
    {
        try { SetAuth(); return await Ok<IEnumerable<RoomDto>>(await httpClient.GetAsync($"api/rooms?wardId={wardId}")); }
        catch (Exception ex) { return Fail<IEnumerable<RoomDto>>(ex.Message); }
    }

    public async Task<ApiResponse<RoomDto>> CreateRoomAsync(CreateRoomDto dto)
    {
        try { SetAuth(); return await Ok<RoomDto>(await httpClient.PostAsJsonAsync("api/rooms", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<RoomDto>(ex.Message); }
    }

    public async Task<ApiResponse<RoomDto>> UpdateRoomAsync(Guid id, UpdateRoomDto dto)
    {
        try { SetAuth(); return await Ok<RoomDto>(await httpClient.PutAsJsonAsync($"api/rooms/{id}", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<RoomDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteRoomAsync(Guid id)
    {
        try { SetAuth(); var r = await httpClient.DeleteAsync($"api/rooms/{id}"); return new ApiResponse<bool> { Success = r.IsSuccessStatusCode, Data = r.IsSuccessStatusCode }; }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }

    // --- Beds ---

    public async Task<ApiResponse<IEnumerable<BedDto>>> GetBedsByRoomAsync(Guid roomId)
    {
        try { SetAuth(); return await Ok<IEnumerable<BedDto>>(await httpClient.GetAsync($"api/beds?roomId={roomId}")); }
        catch (Exception ex) { return Fail<IEnumerable<BedDto>>(ex.Message); }
    }

    public async Task<ApiResponse<IEnumerable<BedDto>>> GetAvailableBedsAsync(Guid? wardId = null)
    {
        try
        {
            SetAuth();
            var url = "api/beds/available" + (wardId.HasValue ? $"?wardId={wardId}" : "");
            return await Ok<IEnumerable<BedDto>>(await httpClient.GetAsync(url));
        }
        catch (Exception ex) { return Fail<IEnumerable<BedDto>>(ex.Message); }
    }

    public async Task<ApiResponse<BedDto>> CreateBedAsync(CreateBedDto dto)
    {
        try { SetAuth(); return await Ok<BedDto>(await httpClient.PostAsJsonAsync("api/beds", dto, JsonOptions)); }
        catch (Exception ex) { return Fail<BedDto>(ex.Message); }
    }

    public async Task<ApiResponse<bool>> DeleteBedAsync(Guid id)
    {
        try { SetAuth(); var r = await httpClient.DeleteAsync($"api/beds/{id}"); return new ApiResponse<bool> { Success = r.IsSuccessStatusCode, Data = r.IsSuccessStatusCode }; }
        catch (Exception ex) { return Fail<bool>(ex.Message); }
    }
}
