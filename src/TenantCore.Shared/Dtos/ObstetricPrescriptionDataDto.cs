namespace TenantCore.Shared.Dtos;

public class ObstetricPrescriptionDataDto
{
    public Guid Id { get; init; }
    public Guid PrescriptionId { get; init; }
    public int? Gravida { get; init; }
    public int? Para { get; init; }
    public int? Live { get; init; }
    public int? Abortion { get; init; }
    public string? Information { get; init; }
    public IReadOnlyList<string> MenstrualHistory { get; init; } = [];
    public IReadOnlyList<string> PastMedicalHistory { get; init; } = [];
    public IReadOnlyList<string> FamilyHistory { get; init; } = [];
    public DateOnly? Lmp { get; init; }
    public DateOnly? EddByLmp { get; init; }
    public DateOnly? EddByUsg { get; init; }
}

public sealed record UpsertObstetricPrescriptionDataDto(
    int? Gravida,
    int? Para,
    int? Live,
    int? Abortion,
    string? Information,
    IReadOnlyList<string>? MenstrualHistory,
    IReadOnlyList<string>? PastMedicalHistory,
    IReadOnlyList<string>? FamilyHistory,
    DateOnly? Lmp = null,
    DateOnly? EddByUsg = null);
