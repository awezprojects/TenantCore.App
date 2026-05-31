using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class DoctorProfileRepository(ClinicDbContext dbContext)
    : ClinicRepository<DoctorProfile>(dbContext), IDoctorProfileRepository
{
    public async Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(dp => dp.UserId == userId, ct);
}
