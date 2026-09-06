using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

// A reusable, named set of medicines (with dose/frequency/duration per drug) that a doctor
// creates once for a recurring clinical situation (e.g. "1st Trimester Care - 30 Days") and
// can insert into any prescription in one action instead of re-adding each drug manually.
public class MedicineBundle : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int DurationDays { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByName { get; private set; } = string.Empty;

    private readonly List<MedicineBundleItem> _items = [];
    public IReadOnlyCollection<MedicineBundleItem> Items => _items.AsReadOnly();

    private MedicineBundle() { }

    public static MedicineBundle Create(
        Guid applicationId,
        string name,
        int durationDays,
        string? notes,
        Guid createdByUserId,
        string createdByName,
        IEnumerable<MedicineBundleItem> items)
    {
        var bundle = new MedicineBundle
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Name = name,
            DurationDays = durationDays,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAt = DateTime.UtcNow
        };
        bundle._items.AddRange(items);
        return bundle;
    }

    public void Update(string name, int durationDays, string? notes, IEnumerable<MedicineBundleItem> items)
    {
        Name = name;
        DurationDays = durationDays;
        Notes = notes;
        _items.Clear();
        _items.AddRange(items);
        SetUpdatedAt();
    }
}
