using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class GrantClinicSubscriptionHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicSubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public GrantClinicSubscriptionHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) =>
                WorkflowTask.Enqueue(t, k, at, aid, p));

        _subscriptionRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private GrantClinicSubscriptionHandler CreateHandler()
        => new(_planRepository.Object, _subscriptionRepository.Object, _workflowEnqueuer.Object,
               Mock.Of<ILogger<GrantClinicSubscriptionHandler>>());

    private static SubscriptionPlan Plan(int durationDays = 30, bool isActive = true)
    {
        var plan = SubscriptionPlan.CreateCustom("Monthly", "d", durationDays, 999m, false, 1, true);
        if (!isActive) plan.Deactivate();
        return plan;
    }

    private GrantClinicSubscriptionCommand Command(Guid planId, ClinicContact? contact = null) => new(
        _applicationId, planId, "Goodwill",
        contact ?? new ClinicContact("Sunrise Clinic", "Dr Mehta", "doctor@example.test", "9876543210"),
        Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_ClinicAlreadyCovered_StartsWhenCurrentCoverageEnds()
    {
        var plan = Plan();
        var coverageEnd = DateTime.UtcNow.AddDays(12);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.GetCoverageEndAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverageEnd);

        ClinicSubscription? added = null;
        _subscriptionRepository.Setup(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicSubscription, CancellationToken>((s, _) => added = s);

        await CreateHandler().Handle(Command(plan.Id), CancellationToken.None);

        added.Should().NotBeNull();
        added!.StartDate.Should().Be(coverageEnd, "a granted term queues after existing coverage — no overlap, no gap");
        added.EndDate.Should().Be(coverageEnd.AddDays(30));
        added.PricePaid.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ClinicNotCovered_StartsImmediately()
    {
        var plan = Plan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.GetCoverageEndAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        ClinicSubscription? added = null;
        _subscriptionRepository.Setup(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicSubscription, CancellationToken>((s, _) => added = s);

        await CreateHandler().Handle(Command(plan.Id), CancellationToken.None);

        added!.StartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        added.IsCurrentlyActive(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidGrant_EnqueuesNotificationEmail()
    {
        var plan = Plan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.GetCoverageEndAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        await CreateHandler().Handle(Command(plan.Id), CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.SendEmail,
            It.Is<string>(k => k.Contains("subscription-granted")),
            nameof(ClinicSubscription),
            It.IsAny<Guid>(),
            It.Is<string?>(p => p != null && p.Contains("SubscriptionGranted")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InactivePlan_ThrowsNotFoundException()
    {
        var plan = Plan(isActive: false);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var act = async () => await CreateHandler().Handle(Command(plan.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MissingPlan_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ContactSnapshot_IsStoredOnTheSubscription()
    {
        var plan = Plan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _subscriptionRepository.Setup(r => r.GetCoverageEndAsync(_applicationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        ClinicSubscription? added = null;
        _subscriptionRepository.Setup(r => r.AddAsync(It.IsAny<ClinicSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<ClinicSubscription, CancellationToken>((s, _) => added = s);

        await CreateHandler().Handle(Command(plan.Id), CancellationToken.None);

        added!.ClinicName.Should().Be("Sunrise Clinic");
        added.BillingContactEmail.Should().Be("doctor@example.test");
        added.BillingContactName.Should().Be("Dr Mehta");
        added.GrantedByAdminEmail.Should().Be("admin@example.test");
    }
}
