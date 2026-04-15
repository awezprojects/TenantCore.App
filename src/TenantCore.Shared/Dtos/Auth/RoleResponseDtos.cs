namespace TenantCore.Shared.Dtos.Auth;

using System.Text.Json.Serialization;

public class RoleResponseDto
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = [];
}

public class ApplicationRolesResponseDto
{
    public List<RoleResponseDto> Roles { get; set; } = [];
}
