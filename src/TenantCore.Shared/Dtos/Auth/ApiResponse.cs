namespace TenantCore.Shared.Dtos.Auth;

/// <summary>
/// Standard API response wrapper for all Auth API responses.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable code from the ProblemDetails <c>errorCode</c> extension (e.g.
    /// <c>insufficient_wallet_balance</c>), populated by the Web.Client response reader so a screen can
    /// react to a specific refusal instead of matching on message text. Null when the server sent none.
    /// </summary>
    public string? ErrorCode { get; set; }

    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>See <see cref="ApiResponse{T}.ErrorCode"/>.</summary>
    public string? ErrorCode { get; set; }

    public List<string> Errors { get; set; } = [];
}
