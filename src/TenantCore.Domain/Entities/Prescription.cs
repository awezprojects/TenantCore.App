using TenantCore.Domain.Common;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class Prescription : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public Guid OpdRegistrationId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string PrescriptionNumber { get; private set; } = string.Empty;
    public DateTime PrescribedDate { get; private set; }
    public DateTime? NextVisitDate { get; private set; }
    public string? Notes { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public bool IsEmailSent { get; private set; }

    private readonly List<PrescriptionItem> _items = [];
    private readonly List<PrescriptionReport> _reports = [];

    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<PrescriptionReport> Reports => _reports.AsReadOnly();

    private Prescription() { }

    public static Prescription Create(
        Guid applicationId,
        Guid opdRegistrationId,
        Guid patientId,
        Guid doctorUserId,
        string doctorName,
        string prescriptionNumber,
        DateTime? nextVisitDate,
        string? notes,
        IEnumerable<PrescriptionItem> items)
    {
        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            OpdRegistrationId = opdRegistrationId,
            PatientId = patientId,
            DoctorUserId = doctorUserId,
            DoctorName = doctorName,
            PrescriptionNumber = prescriptionNumber,
            PrescribedDate = DateTime.UtcNow,
            NextVisitDate = nextVisitDate,
            Notes = notes,
            Status = PrescriptionStatus.Draft,
            IsEmailSent = false,
            CreatedAt = DateTime.UtcNow
        };
        prescription._items.AddRange(items);
        return prescription;
    }

    public void Update(DateTime? nextVisitDate, string? notes, IEnumerable<PrescriptionItem> items)
    {
        if (Status == PrescriptionStatus.Submitted)
            throw new DomainValidationException("Cannot update a submitted prescription.");

        NextVisitDate = nextVisitDate;
        Notes = notes;
        _items.Clear();
        _items.AddRange(items);
        SetUpdatedAt();
    }

    public void Submit()
    {
        if (Status == PrescriptionStatus.Submitted)
            throw new DomainValidationException("Prescription has already been submitted.");

        Status = PrescriptionStatus.Submitted;
        SetUpdatedAt();
    }

    public void MarkEmailSent(bool sent)
    {
        IsEmailSent = sent;
        SetUpdatedAt();
    }

    public void AddReport(PrescriptionReport report) => _reports.Add(report);
}
