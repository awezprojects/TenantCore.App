using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Particulars.Translators;

public static class ParticularTranslator
{
    public static Particular ToEntity(CreateParticularCommand command)
        => Particular.Create(command.ApplicationId, command.Request.Name, command.Request.DefaultAmount);

    public static ParticularDto ToDto(Particular entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        Name = entity.Name,
        DefaultAmount = entity.DefaultAmount,
        IsActive = entity.IsActive
    };

    public static ParticularSummaryDto ToSummaryDto(Particular entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        DefaultAmount = entity.DefaultAmount,
        IsActive = entity.IsActive
    };
}
