using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ClinicUsgTemplate : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public bool IsCustomized { get; set; }

    public ICollection<UsgTemplateRow> Rows { get; set; } = [];
}
