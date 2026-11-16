using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class IpdRegistrationRepository(ClinicDbContext dbContext)
    : ClinicRepository<IpdRegistration>(dbContext), IIpdRegistrationRepository
{
    public async Task<(IEnumerable<IpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(i => i.Patient)
            .Where(i => i.ApplicationId == applicationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.AdmissionNumber.Contains(search) ||
                i.DoctorName.Contains(search) ||
                i.Patient.FirstName.Contains(search) ||
                i.Patient.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.AdmissionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // OBS-1 fix: the admission number's date prefix must match the clinic's LOCAL calendar day
    // (a night admission just after midnight IST showed "yesterday" when the prefix came from
    // UTC). AdmissionDate itself stays UTC — unlike OPD's RegistrationDate, it now also feeds
    // IpdBillCalculator's room-charge math via IpdBedTransferHistory, so switching it to local
    // time would silently shift every room-charge calculation by the server's UTC offset. A
    // narrow 2-UTC-day candidate window (covers any real-world UTC offset) is pulled into memory
    // and filtered by local date there, instead of trying to push a timezone conversion into SQL.
    public async Task<string> GetNextAdmissionNumberAsync(Guid applicationId, CancellationToken ct = default)
    {
        var localToday = DateTime.Now.Date;
        var prefix = $"IPD-{localToday:yyyyMMdd}-";

        var windowStartUtc = DateTime.UtcNow.Date.AddDays(-1);
        var candidates = await DbSet
            .Where(i => i.ApplicationId == applicationId && i.AdmissionDate >= windowStartUtc)
            .Select(i => new { i.AdmissionDate, i.AdmissionNumber })
            .ToListAsync(ct);

        var lastNumber = candidates
            .Where(c => c.AdmissionDate.ToLocalTime().Date == localToday)
            .OrderByDescending(c => c.AdmissionNumber)
            .Select(c => c.AdmissionNumber)
            .FirstOrDefault();

        var nextSequence = 1;
        if (lastNumber is not null && lastNumber.Length > prefix.Length &&
            int.TryParse(lastNumber[prefix.Length..], out var lastSequence))
        {
            nextSequence = lastSequence + 1;
        }

        return $"{prefix}{nextSequence:D4}";
    }

    public async Task<bool> HasActiveAdmissionAsync(Guid patientId, Guid applicationId, CancellationToken ct = default)
        => await DbSet.AnyAsync(
            i => i.PatientId == patientId &&
                 i.ApplicationId == applicationId &&
                 i.Status == TenantCore.Shared.Enums.IpdStatus.Admitted, ct);

    public override async Task<IpdRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IEnumerable<IpdRegistration>> GetDischargesByDateRangeAsync(DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default)
        => await DbSet
            .Include(i => i.Patient)
            .AsNoTracking()
            .Where(i => i.ApplicationId == applicationId &&
                i.Status == TenantCore.Shared.Enums.IpdStatus.Discharged &&
                i.DischargeDate != null && i.DischargeDate >= fromUtc && i.DischargeDate < toUtc)
            .OrderByDescending(i => i.DischargeDate)
            .ToListAsync(ct);
}
