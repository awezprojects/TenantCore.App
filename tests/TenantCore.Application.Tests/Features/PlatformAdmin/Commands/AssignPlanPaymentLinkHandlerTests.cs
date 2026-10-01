using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Handlers;
using TenantCore.Application.Features.PlatformAdmin.Models;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.PlatformAdmin.Commands;

public class AssignPlanPaymentLinkHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<IClinicPlanCatalog> _planCatalog = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public AssignPlanPaymentLinkHandlerTests()
    {
        _paymentGateway.SetupGet(g => g.IsConfigured).Returns(true);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) =>
                WorkflowTask.Enqueue(t, k, at, aid, p));
    }

    private AssignPlanPaymentLinkHandler CreateHandler()
        => new(_planRepository.Object, _paymentRepository.Object, _planCatalog.Object,
               _paymentGateway.Object, _workflowEnqueuer.Object,
               Mock.Of<ILogger<AssignPlanPaymentLinkHandler>>());

    private SubscriptionPlan SetUpPlan(decimal listPrice = 999m, decimal? effectivePrice = null, bool isTrial = false)
    {
        var plan = isTrial
            ? SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Trial, "Trial", "d", 14, 0m, "INR", true, false, 1)
            : SubscriptionPlan.CreateCustom("Monthly", "d", 30, listPrice, false, 1, true);

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _planCatalog.Setup(c => c.GetEffectivePriceAsync(_applicationId, plan, It.IsAny<CancellationToken>()))
            .ReturnsAsync(effectivePrice ?? listPrice);
        return plan;
    }

    private AssignPlanPaymentLinkCommand Command(Guid planId, decimal amount, string? reason = null) => new(
        _applicationId, planId, amount, reason,
        new ClinicContact("Sunrise Clinic", "Dr Mehta", "doctor@example.test", "9876543210"),
        Guid.NewGuid(), "admin@example.test");

    [Fact]
    public async Task Handle_NoOpenLink_CreatesAdminAssignedPaymentAndEnqueuesLinkCreation()
    {
        var plan = SetUpPlan();
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(Command(plan.Id, 999m), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Purpose.Should().Be(PaymentPurpose.AdminAssigned);
        added.ApplicationId.Should().Be(_applicationId);
        added.ClinicName.Should().Be("Sunrise Clinic");
        added.InitiatedByAdminEmail.Should().Be("admin@example.test");

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.Is<string>(k => k.Contains("create-link")),
            nameof(SubscriptionPayment), added.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AmountDiffersFromEffectivePriceWithoutReason_Throws()
    {
        var plan = SetUpPlan(listPrice: 999m);

        var act = async () => await CreateHandler().Handle(Command(plan.Id, 499m), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*reason is required*");
    }

    [Fact]
    public async Task Handle_AmountMatchesOfferPriceNotListPrice_NeedsNoReason()
    {
        // The clinic has a 499 offer on a 999 plan: charging 499 is its normal price, not a discount.
        var plan = SetUpPlan(listPrice: 999m, effectivePrice: 499m);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(Command(plan.Id, 499m), CancellationToken.None);

        added!.Amount.Should().Be(499m);
        added.PlanListPrice.Should().Be(499m, "the comparison baseline is what this clinic would normally pay");
        added.AmountReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AmountDiffersWithReason_Succeeds()
    {
        var plan = SetUpPlan(listPrice: 999m);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPayment?)null);

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(Command(plan.Id, 499m, "Negotiated launch discount"), CancellationToken.None);

        added!.Amount.Should().Be(499m);
        added.AmountReason.Should().Be("Negotiated launch discount");
    }

    [Fact]
    public async Task Handle_OpenLinkWithGatewayLink_CancelsItFirstAndMarksReplacement()
    {
        var plan = SetUpPlan();
        var openLink = SubscriptionPayment.CreateForRenewal(
            _applicationId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, "INR",
            "Dr Mehta", "doctor@example.test", null, Guid.NewGuid());
        openLink.SetLink("plink_abc", "https://rzp.io/x", DateTime.UtcNow.AddDays(7));

        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openLink);

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(Command(plan.Id, 999m), CancellationToken.None);

        openLink.ReplacedByPaymentId.Should().Be(added!.Id);
        openLink.Status.Should().Be(SubscriptionPaymentStatus.LinkCreated, "it is only superseded once Razorpay confirms the cancel");

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, It.Is<string>(k => k.Contains("cancel-link")),
            nameof(SubscriptionPayment), openLink.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OpenLinkStillPending_SupersedesImmediatelyAndCreatesNewLink()
    {
        var plan = SetUpPlan();
        var openLink = SubscriptionPayment.CreateForRenewal(
            _applicationId, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, "INR",
            "Dr Mehta", "doctor@example.test", null, Guid.NewGuid());

        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(_applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openLink);

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(Command(plan.Id, 999m), CancellationToken.None);

        openLink.Status.Should().Be(SubscriptionPaymentStatus.Superseded);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.IsAny<string>(), nameof(SubscriptionPayment), added!.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrialPlan_ThrowsInvalidOperationException()
    {
        var plan = SetUpPlan(isTrial: true);

        var act = async () => await CreateHandler().Handle(Command(plan.Id, 0m), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*grant it*");
    }

    [Fact]
    public async Task Handle_PaymentsNotConfigured_ThrowsInvalidOperationException()
    {
        _paymentGateway.SetupGet(g => g.IsConfigured).Returns(false);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid(), 999m), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not configured*");
    }

    [Fact]
    public async Task Handle_MissingPlan_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid(), 999m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
