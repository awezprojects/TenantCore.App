namespace TenantCore.Shared.Dtos;

public record FrontendErrorLogRequest
{
    public string Message { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
    public string? AdditionalContext { get; init; }
    public Guid? ApplicationId { get; init; }
}
