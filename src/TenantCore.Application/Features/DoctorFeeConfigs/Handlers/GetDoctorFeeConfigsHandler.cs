using MediatR;
using TenantCore.Application.Features.DoctorFeeConfigs.Queries;
using TenantCore.Application.Features.DoctorFeeConfigs.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.DoctorFeeConfigs.Handlers;

public sealed class GetDoctorFeeConfigsHandler(
    IDoctorFeeConfigRepository repository,
    IDoctorProfileRepository doctorProfileRepository,
    IAuthApplicationService authService)
    : IRequestHandler<GetDoctorFeeConfigsQuery, IEnumerable<DoctorFeeConfigSummaryDto>>
{
    public async Task<IEnumerable<DoctorFeeConfigSummaryDto>> Handle(
        GetDoctorFeeConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await repository.GetAllAsync(cancellationToken);
        var appConfigs = configs
            .Where(c => c.ApplicationId == request.ApplicationId)
            .ToList();

        if (appConfigs.Count == 0)
            return [];

        var profileIds = appConfigs.Select(c => c.DoctorProfileId).Distinct();
        var profiles = await doctorProfileRepository.GetByIdsAsync(profileIds, cancellationToken);
        var userIdByProfileId = profiles.ToDictionary(p => p.Id, p => p.UserId);

        var users = await authService.GetApplicationUsersAsync(request.ApplicationId, cancellationToken);
        var nameByUserId = users.ToDictionary(u => u.UserId, u => u.FullName);

        return appConfigs.Select(c =>
        {
            var name = userIdByProfileId.TryGetValue(c.DoctorProfileId, out var userId)
                       && nameByUserId.TryGetValue(userId, out var fullName)
                ? fullName
                : string.Empty;
            return DoctorFeeConfigTranslator.ToSummaryDto(c, name);
        });
    }
}
