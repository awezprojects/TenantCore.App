using TenantCore.Application.Features.CounterSessions.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.CounterSessions.Translators;

public static class CounterSessionTranslator
{
    public static CounterSession ToEntity(OpenCounterSessionCommand command)
        => CounterSession.Create(command.ApplicationId, command.OpenedByUserId, command.Request.SessionDate);

    public static CounterSessionDto ToDto(CounterSession entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        SessionDate = entity.SessionDate,
        OpenedByUserId = entity.OpenedByUserId,
        OpenedAt = DateTime.SpecifyKind(entity.OpenedAt, DateTimeKind.Utc),
        ClosedAt = entity.ClosedAt.HasValue ? DateTime.SpecifyKind(entity.ClosedAt.Value, DateTimeKind.Utc) : null,
        Status = entity.Status,
        TotalCollected = entity.TotalCollected,
        TotalExpenses = entity.TotalExpenses,
        NetAmount = entity.NetAmount
    };

    public static CounterSessionSummaryDto ToSummaryDto(CounterSession entity) => new()
    {
        Id = entity.Id,
        SessionDate = entity.SessionDate,
        OpenedAt = DateTime.SpecifyKind(entity.OpenedAt, DateTimeKind.Utc),
        Status = entity.Status,
        OpenedByUserId = entity.OpenedByUserId,
        TotalCollected = entity.TotalCollected,
        TotalExpenses = entity.TotalExpenses,
        NetAmount = entity.NetAmount
    };

    public static CounterSessionSummaryDto ToSummaryDto(CounterSession entity, decimal liveCollected, decimal liveExpenses) => new()
    {
        Id = entity.Id,
        SessionDate = entity.SessionDate,
        OpenedAt = DateTime.SpecifyKind(entity.OpenedAt, DateTimeKind.Utc),
        Status = entity.Status,
        OpenedByUserId = entity.OpenedByUserId,
        TotalCollected = liveCollected,
        TotalExpenses = liveExpenses,
        NetAmount = liveCollected - liveExpenses
    };
}
