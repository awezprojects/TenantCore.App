namespace TenantCore.Shared.Dtos;

public record SetLmpRequest
{
    public DateOnly Lmp { get; init; }
}
