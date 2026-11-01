using TenantCore.Application.Common;

namespace TenantCore.Infrastructure.Services;

/// <summary>Scoped — one instance per DI scope, i.e. one per workflow-task attempt.</summary>
public sealed class WorkflowCorrelationContext : IWorkflowCorrelationContext
{
    public Guid? CorrelationId { get; set; }
}
