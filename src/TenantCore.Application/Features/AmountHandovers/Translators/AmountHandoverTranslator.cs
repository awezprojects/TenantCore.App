using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.AmountHandovers.Translators;

public static class AmountHandoverTranslator
{
    public static AmountHandover ToEntity(CreateAmountHandoverCommand command)
        => AmountHandover.Create(
            command.ApplicationId,
            command.Request.CounterSessionId,
            command.HandedOverByUserId,
            command.Request.HandedOverToUserId,
            command.Request.HandedOverToRoleName,
            command.Request.Amount,
            command.Request.Notes);

    public static AmountHandoverDto ToDto(AmountHandover entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        CounterSessionId = entity.CounterSessionId,
        HandedOverByUserId = entity.HandedOverByUserId,
        HandedOverToUserId = entity.HandedOverToUserId,
        HandedOverToRoleName = entity.HandedOverToRoleName,
        Amount = entity.Amount,
        Notes = entity.Notes,
        Status = entity.Status,
        HandedOverAt = DateTime.SpecifyKind(entity.HandedOverAt, DateTimeKind.Utc),
        AcceptedAt = entity.AcceptedAt.HasValue ? DateTime.SpecifyKind(entity.AcceptedAt.Value, DateTimeKind.Utc) : null,
        ResolutionNotes = entity.ResolutionNotes
    };

    public static AmountHandoverSummaryDto ToSummaryDto(AmountHandover entity) => new()
    {
        Id = entity.Id,
        Amount = entity.Amount,
        Status = entity.Status,
        HandedOverAt = DateTime.SpecifyKind(entity.HandedOverAt, DateTimeKind.Utc),
        HandedOverByUserId = entity.HandedOverByUserId,
        HandedOverToUserId = entity.HandedOverToUserId,
        HandedOverToRoleName = entity.HandedOverToRoleName
    };
}
