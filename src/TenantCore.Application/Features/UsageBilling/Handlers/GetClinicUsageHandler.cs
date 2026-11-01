using MediatR;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common.Exceptions;
using TenantCore.Application.Features.UsageBilling.Queries;
using TenantCore.Application.Features.UsageBilling.Services;
using TenantCore.Application.Features.UsageBilling.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.UsageBilling;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.UsageBilling.Handlers;

/// <summary>
/// One read for the whole usage screen. The range is fetched once and rolled up here — per-item
/// totals, the daily series and the row list all come from the same two queries, so the client never
/// has to ask again for a tab, a sort or an export.
/// </summary>
public sealed class GetClinicUsageHandler(
    IClinicAccountRepository accountRepository,
    IClinicUsageRecordRepository usageRecordRepository,
    IOptions<UsageBillingOptions> options)
    : IRequestHandler<GetClinicUsageQuery, ClinicUsageDto>
{
    private readonly UsageBillingOptions _options = options.Value;

    public async Task<ClinicUsageDto> Handle(GetClinicUsageQuery query, CancellationToken ct)
    {
        var mode = await accountRepository.GetBillingModeAsync(query.ApplicationId, ct);
        if (mode == BillingMode.Subscription)
            throw new ErrorCodeException(403, "subscription_billing_not_applicable",
                "Usage details are not available for subscription clinics.");

        // The rows and the grouped totals are separate reads on purpose: the roll-up is done in SQL
        // where the index supports it, rather than by pulling a year of rows into memory.
        var records = await usageRecordRepository.GetInRangeAsync(query.ApplicationId, query.From, query.To, ct);
        var totals = await usageRecordRepository.GetTotalsAsync(query.ApplicationId, query.From, query.To, ct);

        // The per-item totals are rolled up in SQL by the repository, so there is no second pass over
        // the rows here — and no second read of the rates either: an unpriced operation is simply one
        // whose charged and notional totals are both zero.
        var ordered = records.OrderByDescending(r => r.ChargedAt).ToList();
        var capped = ordered.Count > _options.MaxUsageRows;

        return UsageBillingTranslator.ToDto(
            capped ? ordered.Take(_options.MaxUsageRows).ToList() : ordered,
            totals, query.From, query.To, capped);
    }
}
