using System.Net.Http.Json;
using System.Text.Json;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Clients;

/// <summary>
/// Shared reader for API responses.
///
/// Several API clients used to copy the raw response body straight into
/// <see cref="ApiResponse{T}.Message"/>. When the API rejects a request without a body
/// (e.g. a 403 from the role-based authorization policy, or a 401), that produced an
/// EMPTY message — which surfaced in the UI as a blank red toast, or as a button that
/// silently did nothing when the page rendered the message inline. This helper turns an
/// unsuccessful response into a message a user can actually read: the ProblemDetails
/// <c>detail</c> when the API sent one, otherwise a status-code based fallback.
/// </summary>
internal static class ApiResponseReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return new ApiResponse<T> { Success = false, Message = await ExtractMessageAsync(response) };

        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResponse<T> { Success = true, Data = data };
    }

    /// <summary>For endpoints that return no payload (204 No Content) or a plain bool.</summary>
    internal static async Task<ApiResponse> ReadAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return new ApiResponse { Success = false, Message = await ExtractMessageAsync(response) };

        return new ApiResponse { Success = true };
    }

    internal static async Task<string> ExtractMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Plain BadRequest(string) actions serialize as a bare JSON string.
                if (root.ValueKind == JsonValueKind.String)
                {
                    var text = root.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }

                foreach (var propertyName in (string[])["detail", "message", "title"])
                {
                    if (root.TryGetProperty(propertyName, out var property))
                    {
                        var text = property.GetString();
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }

                // FluentValidation field errors — flatten to a single readable line.
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
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
        catch { /* fall through to the status-code fallback */ }

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
}
