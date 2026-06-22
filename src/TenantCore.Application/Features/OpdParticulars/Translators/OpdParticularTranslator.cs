using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdParticulars.Translators;

public static class OpdParticularTranslator
{
    public static OpdParticular ToEntity(AddOpdParticularCommand command, string particularName)
        => OpdParticular.Create(
            command.ApplicationId,
            command.Request.OpdRegistrationId,
            command.Request.ParticularId,
            particularName,
            command.Request.Amount);

    public static OpdParticularDto ToDto(OpdParticular entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        OpdRegistrationId = entity.OpdRegistrationId,
        ParticularId = entity.ParticularId,
        ParticularName = entity.ParticularName,
        Amount = entity.Amount,
        PaymentStatus = entity.PaymentStatus,
        CollectedAt = entity.CollectedAt,
        CounterSessionId = entity.CounterSessionId
    };
}
