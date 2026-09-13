using System.Diagnostics;
using System.Reflection;
using MediatR;
using TenantCore.Application.Services;

namespace TenantCore.Application.Common.Behaviors;

/// <summary>
/// Business-action audit trail — distinct from <see cref="LoggingBehavior{TRequest,TResponse}"/>
/// (generic dev diagnostics for every request). Only fires for commands that opt in by
/// implementing <see cref="IBusinessAction"/>: writes a "Started" row before the handler runs
/// and a matching "Completed"/"Failed" row after, tied together by a correlation id. Never
/// swallows or alters the original exception — it is always rethrown unchanged after the
/// "Failed" row is written.
/// </summary>
public sealed class ActionLoggingBehavior<TRequest, TResponse>(
    IActionLogger actionLogger,
    ICurrentUserContext currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Every tenant-scoped command carries this property per the workspace convention
    // (CLAUDE.md: "Always include ApplicationId on tenant-scoped commands/queries") —
    // reflection here avoids requiring IBusinessAction itself to carry it, since not
    // every audited command is tenant-scoped.
    private static readonly PropertyInfo? ApplicationIdProperty =
        typeof(TRequest).GetProperty("ApplicationId", typeof(Guid));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IBusinessAction businessAction)
            return await next();

        var correlationId = Guid.NewGuid();
        var requestType = typeof(TRequest).Name;
        var applicationId = ApplicationIdProperty?.GetValue(request) as Guid?;
        var userId = currentUser.UserId;

        await actionLogger.LogStartedAsync(correlationId, businessAction.ActionName, requestType, applicationId, userId, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            await actionLogger.LogCompletedAsync(correlationId, businessAction.ActionName, requestType, applicationId, userId, stopwatch.ElapsedMilliseconds, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await actionLogger.LogFailedAsync(correlationId, businessAction.ActionName, requestType, applicationId, userId, stopwatch.ElapsedMilliseconds, ex.Message, cancellationToken);
            throw;
        }
    }
}
