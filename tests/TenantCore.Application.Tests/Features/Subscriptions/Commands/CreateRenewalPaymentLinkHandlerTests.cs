using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Features.Subscriptions.Services;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Auth;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

public class CreateRenewalPaymentLinkHandlerTests
{
    private readonly Mock<ISubscriptionPaymentRepository> _paymentRepository = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepository = new();
    private readonly Mock<IClinicAccountRepository> _accountRepository = new();
    private readonly Mock<IClinicPlanCatalog> _planCatalog = new();
    private readonly Mock<IAuthApplicationService> _authApplicationService = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public CreateRenewalPaymentLinkHandlerTests()
    {
        _paymentGateway.Setup(g => g.IsConfigured).Returns(true);
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Defaults: not suspended, and every plan visible at its list price.
        _accountRepository.Setup(r => r.IsSuspendedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _planCatalog.Setup(c => c.IsVisibleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _planCatalog.Setup(c => c.GetEffectivePriceAsync(It.IsAny<Guid>(), It.IsAny<SubscriptionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, SubscriptionPlan p, CancellationToken _) => p.Price);
    }

    private CreateRenewalPaymentLinkHandler CreateHandler() =>
        new(_paymentRepository.Object, _planRepository.Object, _accountRepository.Object, _planCatalog.Object,
            _authApplicationService.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

    private static SubscriptionPlan CreatePlan(bool isTrial = false, decimal price = 999m) =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), isTrial ? SubscriptionPlanCode.Trial : SubscriptionPlanCode.Monthly,
            isTrial ? "Trial" : "Monthly", "d", 30, price, "INR", isTrial, false, 1);

    private void SetupAuthServices(Guid applicationId, Guid userId)
    {
        _authApplicationService.Setup(s => s.GetApplicationByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationResponseDto { ApplicationId = applicationId, ApplicationName = "Sunrise Clinic" });
        _authApplicationService.Setup(s => s.GetApplicationUsersAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ApplicationUserResponseDto { UserId = userId, FullName = "Admin", EmailId = "admin@sunrise.test" }]);
    }

    [Fact]
    public async Task Handle_PaidPlan_CreatesPaymentAndEnqueuesCreatePaymentLink()
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = CreatePlan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        SetupAuthServices(applicationId, userId);

        var dto = await CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, userId), CancellationToken.None);

        dto.Amount.Should().Be(999m);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Once);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CreatePaymentLink, It.IsAny<string>(), nameof(SubscriptionPayment), It.IsAny<Guid>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrialPlan_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        var trialPlan = CreatePlan(isTrial: true, price: 0m);
        _planRepository.Setup(r => r.GetByIdAsync(trialPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trialPlan);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, trialPlan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OpenLinkForTheSamePlanAndAmount_ReturnsExistingInsteadOfCreatingNew()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan();
        var existing = SubscriptionPayment.CreateForRenewal(applicationId, plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Admin", "admin@sunrise.test", "9876543210", Guid.NewGuid());
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var dto = await CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        dto.Id.Should().Be(existing.Id);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OpenLinkForADifferentPlan_SupersedesItAndCreatesANewOne()
    {
        // The bug this covers: choosing a different plan used to hand back the OLD plan's link,
        // so a clinic could never switch once a link existed.
        var applicationId = Guid.NewGuid();
        var oldPlan = CreatePlan(price: 999m);
        var newPlan = SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Quarterly, "Quarterly", "d", 90, 2499m, "INR", false, true, 3);

        var existing = SubscriptionPayment.CreateForRenewal(applicationId, oldPlan.Id, oldPlan.Code, oldPlan.Name, oldPlan.Price, "INR", "Admin", "admin@sunrise.test", null, Guid.NewGuid());
        existing.SetLink("plink_old", "https://rzp.io/old", DateTime.UtcNow.AddDays(7));

        _planRepository.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newPlan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupAuthServices(applicationId, Guid.NewGuid());

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        var dto = await CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, newPlan.Id, Guid.NewGuid()), CancellationToken.None);

        dto.Id.Should().Be(added!.Id);
        added.Amount.Should().Be(2499m);
        existing.ReplacedByPaymentId.Should().Be(added.Id);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, It.IsAny<string>(), nameof(SubscriptionPayment), existing.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OpenAdminAssignedLink_IsRefused()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan();
        var adminLink = SubscriptionPayment.CreateForAdminAssignment(
            applicationId, plan.Id, plan.Code, plan.Name, 499m, 999m, "INR",
            "Sunrise Clinic", "Admin", "admin@sunrise.test", null, "platform@example.test", "Negotiated");
        adminLink.SetLink("plink_admin", "https://rzp.io/admin", DateTime.UtcNow.AddDays(7));

        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(adminLink);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*CloudClinic has sent*");
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClinicWithAnOffer_ChargesTheOfferPriceAndKeepsTheListPrice()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan(price: 999m);
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        _planCatalog.Setup(c => c.GetEffectivePriceAsync(applicationId, plan, It.IsAny<CancellationToken>())).ReturnsAsync(499m);
        SetupAuthServices(applicationId, Guid.NewGuid());

        SubscriptionPayment? added = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPayment, CancellationToken>((p, _) => added = p);

        await CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        added!.Amount.Should().Be(499m);
        added.PlanListPrice.Should().Be(999m);
    }

    [Fact]
    public async Task Handle_PlanNotVisibleToThisClinic_ThrowsNotFoundException()
    {
        // A private package must not even be confirmed to exist to a clinic without an offer.
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _planCatalog.Setup(c => c.IsVisibleAsync(applicationId, plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SuspendedClinic_ThrowsInvalidOperationException()
    {
        var applicationId = Guid.NewGuid();
        _accountRepository.Setup(r => r.IsSuspendedAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*suspended*");
        _planRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LooksUpOpenLinkScopedToTheCommandsOwnApplicationId_NotAnyOtherClinic()
    {
        var applicationId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        var plan = CreatePlan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        // Another clinic's open link must never leak into this call, however the fake is set up.
        _paymentRepository.Setup(r => r.GetOpenLinkForClinicAsync(otherClinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionPayment.CreateForRenewal(otherClinicId, plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Other", "other@x.test", null, Guid.NewGuid()));
        SetupAuthServices(applicationId, Guid.NewGuid());

        await CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        _paymentRepository.Verify(r => r.GetOpenLinkForClinicAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.GetOpenLinkForClinicAsync(otherClinicId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaymentsNotConfigured_ThrowsInvalidOperationException()
    {
        _paymentGateway.Setup(g => g.IsConfigured).Returns(false);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _planRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanNotFoundOrInactive_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPlan?)null);

        var action = () => CreateHandler().Handle(new CreateRenewalPaymentLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
