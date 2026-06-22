namespace TenantCore.Shared.Dtos;

public class ExpenseCategoryDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public class ExpenseCategorySummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateExpenseCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed record UpdateExpenseCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}
