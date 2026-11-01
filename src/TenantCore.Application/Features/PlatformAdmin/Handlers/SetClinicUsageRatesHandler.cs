using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Internal admin setting this clinic's rates. Deactivate-and-insert rather than update-in-place, so
/// the history of what a clinic was charged when survives — a usage row snapshots its own rate, and
/// this keeps the surrounding story consistent with it.
/// </summary>
public sealed class SetClinicUsageRatesHandler(
    IClinicUsageRateRepository rateRepository,
    ILogger<SetClinicUsageRatesHandler> logger)
    : IRequestHandler<SetClinicUsageRatesCommand>
{
    public async Task Handle(SetClinicUsageRatesCommand command, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var changed = new List<UsageRateSetting>();

        foreach (var setting in command.Rates)
        {
            var active = await rateRepository.GetActiveAsync(command.ApplicationId, setting.BillingItem, ct);

            // The same price is the same row. Rewriting it would fill the rate history with identical
            // entries and make the admin audit unreadable, so an unchanged rate is a no-op.
            if (active is not null && active.Rate == setting.Rate)
                continue;

            active?.Deactivate();

            var replacement = ClinicUsageRate.Create(
                command.ApplicationId, setting.BillingItem, setting.Rate, utcNow, command.AdminEmail);

            await rateRepository.AddAsync(replacement, ct);
            changed.Add(setting);
        }

        if (changed.Count == 0)
        {
            logger.LogInformation(
                "Clinic {ApplicationId} rates unchanged — no rows written.", command.ApplicationId);
            return;
        }

        // One SaveChanges for the whole set: the deactivations and their replacements land together,
        // so the (clinic, item) filtered unique index is never momentarily satisfied twice.
        await rateRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Clinic {ApplicationId} rates set by {AdminEmail}: {Rates}.",
            command.ApplicationId, command.AdminEmail,
            string.Join(", ", changed.Select(c => $"{c.BillingItem}={c.Rate}")));
    }
}
