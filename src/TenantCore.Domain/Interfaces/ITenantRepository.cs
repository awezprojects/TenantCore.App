using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

/// <summary>
/// Repository interface for Tenant-specific operations.
/// Inherits common CRUD operations from IRepository.
/// </summary>
public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
}
