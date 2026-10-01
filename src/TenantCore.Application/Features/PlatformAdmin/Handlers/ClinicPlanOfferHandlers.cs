using MediatR;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Gives one clinic access to one plan, optionally at a price only that clinic gets. This is how a
/// private package is sold, and how a discount is granted for a while.
/// </summary>
public sealed class CreateClinicPlanOfferHandler(
    ISubscriptionPlanRepository planRepository,
    IClinicPlanOfferRepository offerRepository)
    : IRequestHandler<CreateClinicPlanOfferCommand, Guid>
{
    public async Task<Guid> Handle(CreateClinicPlanOfferCommand command, CancellationToken ct)
    {
        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);

        // The Trial is free and once-per-clinic; a priced offer for it would be meaningless.
        if (plan.IsTrial)
            throw new InvalidOperationException("The Trial plan cannot be offered — it is free and granted once per clinic.");

        var existing = await offerRepository.GetActiveForClinicAndPlanAsync(command.ApplicationId, plan.Id, ct);
        if (existing is not null)
            throw new InvalidOperationException("This clinic already has a live offer for that plan. Withdraw it first.");

        var offer = ClinicPlanOffer.Create(
            command.ApplicationId, plan.Id, command.OfferPrice, command.ValidUntil,
            PlatformAdminTranslator.NormalizeOptionalText(command.Note), command.AdminEmail);

        await offerRepository.AddAsync(offer, ct);
        await offerRepository.SaveChangesAsync(ct);

        return offer.Id;
    }
}

public sealed class WithdrawClinicPlanOfferHandler(IClinicPlanOfferRepository offerRepository)
    : IRequestHandler<WithdrawClinicPlanOfferCommand>
{
    public async Task Handle(WithdrawClinicPlanOfferCommand command, CancellationToken ct)
    {
        var offer = await offerRepository.GetByIdForClinicAsync(command.OfferId, command.ApplicationId, ct);

        // An offer belonging to another clinic is treated as not found — never leak it.
        if (offer is null)
            throw new NotFoundException(nameof(ClinicPlanOffer), command.OfferId);

        // Throws InvalidOperationException (→ 409) when it was already withdrawn.
        offer.Withdraw(command.AdminEmail);
        await offerRepository.SaveChangesAsync(ct);
    }
}
