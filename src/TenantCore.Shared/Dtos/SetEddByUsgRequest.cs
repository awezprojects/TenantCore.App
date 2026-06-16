namespace TenantCore.Shared.Dtos;

public record SetEddByUsgRequest
{
    public DateOnly EddByUsg { get; init; }
}
