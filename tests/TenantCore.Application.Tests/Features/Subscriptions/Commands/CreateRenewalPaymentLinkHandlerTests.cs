using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
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
    }

    private CreateRenewalPaymentLinkHandler CreateHandler() =>
        new(_paymentRepository.Object, _planRepository.Object, _authApplicationService.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

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
        _paymentRepository.Setup(r => r.GetOpenRenewalForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        SetupAuthServices(applicationId, userId);

        var handler = CreateHandler();
        var dto = await handler.Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, userId), CancellationToken.None);

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

        var handler = CreateHandler();
        var action = () => handler.Handle(new CreateRenewalPaymentLinkCommand(applicationId, trialPlan.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingOpenLink_ReturnsExistingInsteadOfCreatingNew()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan();
        var existing = SubscriptionPayment.CreateForRenewal(applicationId, plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Admin", "admin@sunrise.test", "9876543210", Guid.NewGuid());
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenRenewalForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var dto = await handler.Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        dto.Id.Should().Be(existing.Id);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<SubscriptionPayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LooksUpOpenLinkScopedToTheCommandsOwnApplicationId_NotAnyOtherClinic()
    {
        var applicationId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        var plan = CreatePlan();
        _planRepository.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _paymentRepository.Setup(r => r.GetOpenRenewalForClinicAsync(applicationId, It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPayment?)null);
        // Another clinic's open link must never leak into this call, however the fake is set up.
        _paymentRepository.Setup(r => r.GetOpenRenewalForClinicAsync(otherClinicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionPayment.CreateForRenewal(otherClinicId, plan.Id, plan.Code, plan.Name, plan.Price, "INR", "Other", "other@x.test", null, Guid.NewGuid()));
        SetupAuthServices(applicationId, Guid.NewGuid());

        var handler = CreateHandler();
        await handler.Handle(new CreateRenewalPaymentLinkCommand(applicationId, plan.Id, Guid.NewGuid()), CancellationToken.None);

        _paymentRepository.Verify(r => r.GetOpenRenewalForClinicAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.GetOpenRenewalForClinicAsync(otherClinicId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaymentsNotConfigured_ThrowsInvalidOperationException()
    {
        _paymentGateway.Setup(g => g.IsConfigured).Returns(false);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CreateRenewalPaymentLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _planRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PlanNotFoundOrInactive_ThrowsNotFoundException()
    {
        _planRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SubscriptionPlan?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CreateRenewalPaymentLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
