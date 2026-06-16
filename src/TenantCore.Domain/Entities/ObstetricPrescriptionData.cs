using TenantCore.Domain.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Domain.Entities;

public class ObstetricPrescriptionData : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public int? Gravida { get; private set; }
    public int? Para { get; private set; }
    public int? Live { get; private set; }
    public int? Abortion { get; private set; }
    public string? Information { get; private set; }
    public string? MenstrualHistory { get; private set; }
    public string? PastMedicalHistory { get; private set; }
    public string? FamilyHistory { get; private set; }
    public DateOnly? Lmp { get; private set; }
    public DateOnly? EddByLmp { get; private set; }
    public DateOnly? EddByUsg { get; private set; }

    private ObstetricPrescriptionData() { }

    public static ObstetricPrescriptionData CreateOrUpdate(
        Guid prescriptionId,
        UpsertObstetricPrescriptionDataDto dto)
    {
        var entity = new ObstetricPrescriptionData
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            Gravida = dto.Gravida,
            Para = dto.Para,
            Live = dto.Live,
            Abortion = dto.Abortion,
            Information = dto.Information,
            MenstrualHistory = SerializeList(dto.MenstrualHistory),
            PastMedicalHistory = SerializeList(dto.PastMedicalHistory),
            FamilyHistory = SerializeList(dto.FamilyHistory),
            CreatedAt = DateTime.UtcNow
        };
        if (dto.Lmp.HasValue) entity.SetLmp(dto.Lmp.Value);
        if (dto.EddByUsg.HasValue) entity.EddByUsg = dto.EddByUsg.Value;
        return entity;
    }

    public void Update(UpsertObstetricPrescriptionDataDto dto)
    {
        Gravida = dto.Gravida;
        Para = dto.Para;
        Live = dto.Live;
        Abortion = dto.Abortion;
        Information = dto.Information;
        MenstrualHistory = SerializeList(dto.MenstrualHistory);
        PastMedicalHistory = SerializeList(dto.PastMedicalHistory);
        FamilyHistory = SerializeList(dto.FamilyHistory);
        if (dto.Lmp.HasValue) SetLmp(dto.Lmp.Value);
        if (dto.EddByUsg.HasValue) EddByUsg = dto.EddByUsg.Value;
        SetUpdatedAt();
    }

    public void SetLmp(DateOnly lmp)
    {
        Lmp = lmp;
        EddByLmp = lmp.AddDays(280);
        SetUpdatedAt();
    }

    public void SetEddByUsg(DateOnly edd)
    {
        EddByUsg = edd;
        SetUpdatedAt();
    }

    private static string? SerializeList(IReadOnlyList<string>? list)
    {
        if (list is null || list.Count == 0) return null;
        return System.Text.Json.JsonSerializer.Serialize(list);
    }
}
