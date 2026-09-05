namespace TenantCore.Shared.Dtos;

public class ClinicLocationDto
{
    public Guid ApplicationId { get; init; }
    public Guid StateId { get; init; }
    public string StateName { get; init; } = string.Empty;
    public Guid CityId { get; init; }
    public string CityName { get; init; } = string.Empty;
}

public sealed record UpsertClinicLocationDto(Guid StateId, Guid CityId);
