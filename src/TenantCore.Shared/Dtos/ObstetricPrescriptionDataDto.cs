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
    public IReadOnlyList<HistoryItemSelectionDto> MenstrualHistory { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> PastMedicalHistory { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> FamilyHistory { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> PerAbdomen { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> PerVaginum { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> SurgicalHistory { get; init; } = [];
    public IReadOnlyList<HistoryItemSelectionDto> PerSpeculum { get; init; } = [];
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
    IReadOnlyList<HistoryItemSelectionDto>? MenstrualHistory,
    IReadOnlyList<HistoryItemSelectionDto>? PastMedicalHistory,
    IReadOnlyList<HistoryItemSelectionDto>? FamilyHistory,
    DateOnly? Lmp = null,
    DateOnly? EddByUsg = null,
    IReadOnlyList<HistoryItemSelectionDto>? PerAbdomen = null,
    IReadOnlyList<HistoryItemSelectionDto>? PerVaginum = null,
    IReadOnlyList<HistoryItemSelectionDto>? SurgicalHistory = null,
    IReadOnlyList<HistoryItemSelectionDto>? PerSpeculum = null);
