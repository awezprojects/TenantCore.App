using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Infrastructure.ExternalServices;

public sealed class AuthClinicService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthClinicService> logger)
    : IAuthClinicService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("AuthApi");
        var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        return client;
    }

    public async Task<ApplicationResponseDto> CreateClinicAsync(CreateClinicRequestDto request, CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/clinic", request, JsonOptions, ct);
        return await ParseRequiredAsync<ApplicationResponseDto>(response, ct);
    }

    public async Task<List<ClinicDashboardItemDto>> GetClinicDashboardAsync(CancellationToken ct = default)
    {
        using var client = CreateClient();
        var response = await client.GetAsync("api/clinic/dashboard", ct);
        return await ParseRequiredAsync<List<ClinicDashboardItemDto>>(response, ct);
    }

    private async Task<T> ParseRequiredAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Auth API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Auth API error ({(int)response.StatusCode}): {body}");
        }

        try
        {
            var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
            if (wrapper is null || (!wrapper.Success && wrapper.Data is null))
                throw new InvalidOperationException(wrapper?.Message ?? "Unexpected empty response from Auth API.");
            return wrapper.Data!;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize Auth API response: {Body}", body);
            throw new InvalidOperationException("Failed to parse response from Auth API.", ex);
        }
    }
}
