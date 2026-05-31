using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

public interface IOpdRegistrationRepository : IClinicRepository<OpdRegistration>
{
    Task<(IEnumerable<OpdRegistration> Items, int Total)> GetPagedAsync(
        Guid applicationId, int page, int pageSize, string? search,
        Guid? doctorUserId = null, bool todayOnly = false,
        DateTime? fromDate = null, DateTime? toDate = null,
        OpdStatus? statusFilter = null, bool notVisited = false,
        CancellationToken ct = default);

    Task<int> CountTodayAsync(Guid applicationId, CancellationToken ct = default);

    Task<int> CountTodayByDoctorAsync(Guid applicationId, Guid doctorUserId, CancellationToken ct = default);
}
