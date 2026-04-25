using Microsoft.EntityFrameworkCore;
using TenantCore.Domain.Common;
using TenantCore.Domain.Interfaces;
using TenantCore.Infrastructure.Persistence;

namespace TenantCore.Infrastructure.Repositories;

public class ClinicRepository<T>(ClinicDbContext dbContext) : IClinicRepository<T>
    where T : BaseEntity
{
    protected readonly ClinicDbContext DbContext = dbContext;
    protected readonly DbSet<T> DbSet = dbContext.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Delete(T entity) => DbSet.Remove(entity);

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await DbContext.SaveChangesAsync(cancellationToken);
}
