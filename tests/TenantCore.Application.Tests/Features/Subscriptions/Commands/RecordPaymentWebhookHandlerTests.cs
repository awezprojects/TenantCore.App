using FluentAssertions;
using Moq;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Handlers;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Subscriptions.Commands;

public class RecordPaymentWebhookHandlerTests
{
    private readonly Mock<IPaymentWebhookEventRepository> _webhookEventRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IWorkflowEnqueuer> _workflowEnqueuer = new();

    public RecordPaymentWebhookHandlerTests()
    {
        _workflowEnqueuer
            .Setup(w => w.EnqueueAsync(It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTaskType t, string k, string at, Guid aid, string? p, CancellationToken _) => WorkflowTask.Enqueue(t, k, at, aid, p));
        _webhookEventRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RecordPaymentWebhookHandler CreateHandler() => new(_webhookEventRepository.Object, _paymentGateway.Object, _workflowEnqueuer.Object);

    private const string RawBody = """{"event":"payment_link.paid","payload":{}}""";

    [Fact]
    public async Task Handle_BadSignature_ThrowsUnauthorizedAccessException()
    {
        _paymentGateway.Setup(g => g.VerifyWebhookSignature(RawBody, "bad-signature")).Returns(false);

        var handler = CreateHandler();
        var action = () => handler.Handle(new RecordPaymentWebhookCommand(RawBody, "bad-signature", "evt_1"), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        _webhookEventRepository.Verify(r => r.TryAddAsync(It.IsAny<PaymentWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewEvent_StoresItAndEnqueuesProcessWebhookEvent()
    {
        _paymentGateway.Setup(g => g.VerifyWebhookSignature(RawBody, "good-signature")).Returns(true);
        PaymentWebhookEvent? saved = null;
        _webhookEventRepository.Setup(r => r.TryAddAsync(It.IsAny<PaymentWebhookEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentWebhookEvent, CancellationToken>((e, _) => saved = e)
            .ReturnsAsync(true);

        var handler = CreateHandler();
        await handler.Handle(new RecordPaymentWebhookCommand(RawBody, "good-signature", "evt_1"), CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.EventId.Should().Be("evt_1");
        saved.EventType.Should().Be("payment_link.paid");
        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            WorkflowTaskType.ProcessWebhookEvent, $"process-webhook:{saved.Id}", nameof(PaymentWebhookEvent), saved.Id,
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _webhookEventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEvent_ReturnsSuccessfullyWithoutEnqueueingAnything()
    {
        _paymentGateway.Setup(g => g.VerifyWebhookSignature(RawBody, "good-signature")).Returns(true);
        _webhookEventRepository.Setup(r => r.TryAddAsync(It.IsAny<PaymentWebhookEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        await handler.Handle(new RecordPaymentWebhookCommand(RawBody, "good-signature", "evt_1"), CancellationToken.None);

        _workflowEnqueuer.Verify(w => w.EnqueueAsync(
            It.IsAny<WorkflowTaskType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookEventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
