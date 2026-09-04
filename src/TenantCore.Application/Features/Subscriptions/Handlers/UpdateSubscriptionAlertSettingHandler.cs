using MediatR;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class UpdateSubscriptionAlertSettingHandler(ISubscriptionAlertSettingRepository settingRepository)
    : IRequestHandler<UpdateSubscriptionAlertSettingCommand, SubscriptionAlertSettingDto>
{
    public async Task<SubscriptionAlertSettingDto> Handle(UpdateSubscriptionAlertSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByIdAsync(request.Id, cancellationToken);
        if (setting is null)
            throw new NotFoundException(nameof(SubscriptionAlertSetting), request.Id);

        SubscriptionAlertSettingTranslator.ApplyUpdate(setting, request.Request);
        settingRepository.Update(setting);
        await settingRepository.SaveChangesAsync(cancellationToken);

        return SubscriptionAlertSettingTranslator.ToDto(setting);
    }
}
