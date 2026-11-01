using System.Diagnostics;
using System.Reflection;
using System.Text;
using MediatR;
using TenantCore.Application.Services;

namespace TenantCore.Application.Common.Behaviors;

/// <summary>
/// Business-action audit trail — distinct from <see cref="LoggingBehavior{TRequest,TResponse}"/>
/// (generic dev diagnostics for every request). ON BY DEFAULT (ADR-011): every request whose type
/// name ends in "Command", plus anything implementing <see cref="IBusinessAction"/>, writes a
/// "Started" row before the handler runs and a matching "Completed"/"Failed" row after, tied
/// together by a correlation id. Queries stay silent (reads are covered by the request log when
/// they fail or are slow). <see cref="ISkipActionLog"/> opts a command out. Never swallows or
/// alters the original exception — it is always rethrown unchanged after the "Failed" row.
/// </summary>
public sealed class ActionLoggingBehavior<TRequest, TResponse>(
    IActionLogger actionLogger,
    ICurrentUserContext currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Every tenant-scoped command carries this property per the workspace convention
    // (CLAUDE.md: "Always include ApplicationId on tenant-scoped commands/queries") —
    // reflection here avoids requiring an interface for it, since not every command is tenant-scoped.
    private static readonly PropertyInfo? ApplicationIdProperty =
        typeof(TRequest).GetProperty("ApplicationId", typeof(Guid)) ?? typeof(TRequest).GetProperty("ApplicationId", typeof(Guid?));

    private static readonly string RequestTypeName = typeof(TRequest).Name;

    /// <summary>True when this request type is action-logged (convention or opt-in, minus opt-out).</summary>
    public static bool IsLogged(object request) =>
        request is not ISkipActionLog &&
        (request is IBusinessAction || RequestTypeName.EndsWith("Command", StringComparison.Ordinal));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!IsLogged(request))
            return await next();

        var correlationId = Guid.NewGuid();
        var actionName = (request as IBusinessAction)?.ActionName ?? Humanize(RequestTypeName);

        // requestType carries optional identifier-only context after " | " (see IActionLogger).
        var context = (request as IActionLogContext)?.ActionLogContext;
        var requestType = string.IsNullOrWhiteSpace(context) ? RequestTypeName : $"{RequestTypeName} | {context}";

        var applicationId = ApplicationIdProperty?.GetValue(request) as Guid?;
        var userId = currentUser.UserId;

        await actionLogger.LogStartedAsync(correlationId, actionName, requestType, applicationId, userId, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            await actionLogger.LogCompletedAsync(correlationId, actionName, requestType, applicationId, userId, stopwatch.ElapsedMilliseconds, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await actionLogger.LogFailedAsync(correlationId, actionName, requestType, applicationId, userId, stopwatch.ElapsedMilliseconds, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>"CreatePatientCommand" → "Create Patient"; "AcceptOpdPaymentFullCommand" → "Accept Opd Payment Full".</summary>
    public static string Humanize(string typeName)
    {
        var name = typeName.EndsWith("Command", StringComparison.Ordinal) ? typeName[..^"Command".Length] : typeName;
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.Length == 0 ? typeName : sb.ToString();
    }
}
