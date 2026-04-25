using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
            var err = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T> { Success = false, Message = err };
        }
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    private static ApiResponse<T> Fail<T>(string message) =>
        new() { Success = false, Message = message, Errors = [message] };

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

    // --- OPD Registrations ---

    public async Task<ApiResponse<PagedResult<OpdRegistrationDto>>> GetOpdRegistrationsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        try
        {
            SetAuth();
            var url = $"api/opd-registrations?page={page}&pageSize={pageSize}" +
                      (search is not null ? $"&search={Uri.EscapeDataString(search)}" : "");
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

    public async Task<ApiResponse<IEnumerable<DoctorDto>>> GetDoctorsAsync()
    {
        try
        {
            SetAuth();
            return await Ok<IEnumerable<DoctorDto>>(await httpClient.GetAsync("api/clinic-settings/doctors"));
        }
        catch (Exception ex) { return Fail<IEnumerable<DoctorDto>>(ex.Message); }
    }
}
