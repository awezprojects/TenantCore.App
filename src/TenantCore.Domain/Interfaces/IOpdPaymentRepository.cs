using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IOpdPaymentRepository : IClinicRepository<OpdPayment>
{
    Task<OpdPayment?> GetByOpdRegistrationIdAsync(Guid opdRegistrationId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<OpdPayment>> GetBySessionIdAsync(Guid sessionId, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<OpdPayment>> GetByDateRangeAsync(DateTime from, DateTime to, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<OpdPayment>> GetByOpdRegistrationIdsAsync(IEnumerable<Guid> opdRegistrationIds, Guid applicationId, CancellationToken ct = default);
    Task<IEnumerable<(Guid DoctorUserId, string DoctorName, decimal Total)>> GetDoctorWiseTotalsAsync(DateTime from, DateTime to, Guid applicationId, Guid? doctorUserId, CancellationToken ct = default);

    Task<IEnumerable<(Guid OpdRegistrationId, string RegistrationNumber, string PatientName, string DoctorName, decimal DiscountAmount, Guid? DiscountedByUserId, DateTime? DiscountedAt)>> GetDiscountsAsync(
        DateTime fromUtc, DateTime toUtc, Guid applicationId, CancellationToken ct = default);
}
