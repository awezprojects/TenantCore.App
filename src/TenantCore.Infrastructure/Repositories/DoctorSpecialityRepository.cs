using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class DoctorSpecialityRepository(ClinicDbContext dbContext)
    : ClinicRepository<DoctorSpeciality>(dbContext), IDoctorSpecialityRepository
{
    public async Task<List<DoctorSpeciality>> GetAllActiveAsync(CancellationToken ct = default)
        => await DbSet
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);
}
