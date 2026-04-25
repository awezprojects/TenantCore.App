using TenantCore.Domain.Common;

namespace TenantCore.Domain.Interfaces;

public interface IClinicRepository<T> : IRepository<T> where T : BaseEntity
{
}
