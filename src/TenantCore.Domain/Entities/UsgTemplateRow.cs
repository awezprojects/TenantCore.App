using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class UsgTemplateRow : BaseEntity
{
    public Guid ClinicUsgTemplateId { get; set; }
    public int RowOrder { get; set; }
    public string WeekLabel { get; set; } = string.Empty;
    public int LmpDayOffset { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string Indication { get; set; } = string.Empty;
}
