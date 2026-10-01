using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>Adds an admin-defined package to the global catalogue. Always Code = Custom.</summary>
public sealed class CreateSubscriptionPlanHandler(
    ISubscriptionPlanRepository planRepository,
    IOptions<OnboardingOptions> options,
    ILogger<CreateSubscriptionPlanHandler> logger)
    : IRequestHandler<CreateSubscriptionPlanCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubscriptionPlanCommand command, CancellationToken ct)
    {
        var details = command.Details;

        await EnsureNameIsFreeAsync(planRepository, details.Name, null, ct);
        EnsurePaidPlanMeetsMinimum(details.Price, options.Value.MinPaymentAmount);

        var plan = SubscriptionPlan.CreateCustom(
            details.Name, details.Description, details.DurationDays, details.Price,
            details.IsPopular, details.DisplayOrder, details.IsPublic);

        await planRepository.AddAsync(plan, ct);
        await planRepository.SaveChangesAsync(ct);

        logger.LogInformation("Admin created subscription plan {PlanId}.", plan.Id);
        return plan.Id;
    }

    internal static async Task EnsureNameIsFreeAsync(
        ISubscriptionPlanRepository planRepository, string name, Guid? excludeId, CancellationToken ct)
    {
        if (await planRepository.NameExistsAsync(name, excludeId, ct))
            throw new InvalidOperationException($"A plan named '{name}' already exists.");
    }

    /// <summary>
    /// A paid plan must be chargeable through Razorpay, whose minimum this configuration mirrors.
    /// The free Trial is exempt — its price is pinned at 0 by the entity itself.
    /// </summary>
    internal static void EnsurePaidPlanMeetsMinimum(decimal price, decimal minimum)
    {
        if (price < minimum)
            throw new InvalidOperationException($"A paid plan must cost at least {minimum:N2}.");
    }
}

public sealed class UpdateSubscriptionPlanHandler(
    ISubscriptionPlanRepository planRepository,
    IOptions<OnboardingOptions> options)
    : IRequestHandler<UpdateSubscriptionPlanCommand>
{
    public async Task Handle(UpdateSubscriptionPlanCommand command, CancellationToken ct)
    {
        var plan = await planRepository.GetByIdAsync(command.PlanId, ct);
        if (plan is null)
            throw new NotFoundException(nameof(SubscriptionPlan), command.PlanId);

        var details = command.Details;
        await CreateSubscriptionPlanHandler.EnsureNameIsFreeAsync(planRepository, details.Name, plan.Id, ct);

        if (!plan.IsTrial)
            CreateSubscriptionPlanHandler.EnsurePaidPlanMeetsMinimum(details.Price, options.Value.MinPaymentAmount);

        // Throws InvalidOperationException (→ 409) if this is the Trial and the price is not 0.
        // Existing subscriptions and open links keep their snapshotted values either way.
        plan.UpdateDetails(
            details.Name, details.Description, details.DurationDays, details.Price,
            details.IsPopular, details.DisplayOrder, details.IsPublic);

        planRepository.Update(plan);
        await planRepository.SaveChangesAsync(ct);
    }
}

public sealed class SetSubscriptionPlanActiveHandler(ISubscriptionPlanRepository planRepository)
    : IRequestHandler<SetSubscriptionPlanActiveCommand>
{
    public async Task Handle(SetSubscriptionPlanActiveCommand command, CancellationToken ct)
    {
        var plan = await planRepository.GetByIdAsync(command.PlanId, ct);
        if (plan is null)
            throw new NotFoundException(nameof(SubscriptionPlan), command.PlanId);

        if (command.IsActive)
            plan.Activate();
        else
            plan.Deactivate();

        planRepository.Update(plan);
        await planRepository.SaveChangesAsync(ct);
    }
}
