namespace TenantCore.Shared.Dtos;

public class CityDto
{
    public Guid Id { get; init; }
    public Guid StateId { get; init; }
    public string Name { get; init; } = string.Empty;
}
