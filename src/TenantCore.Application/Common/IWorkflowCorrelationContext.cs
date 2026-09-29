namespace TenantCore.Application.Common;

/// <summary>
/// Carries the current workflow-task attempt's correlation id (set by WorkflowTaskProcessor at
/// the start of each attempt, in the same DI scope the task's handler and its collaborators run
/// in) so an outgoing HTTP call made from the background worker — which has no HttpContext to
/// draw one from — can still be cross-referenced with that attempt's ActionLogs entry and the
/// downstream service's own logs. Null outside a workflow-task attempt.
/// </summary>
public interface IWorkflowCorrelationContext
{
    Guid? CorrelationId { get; set; }
}
