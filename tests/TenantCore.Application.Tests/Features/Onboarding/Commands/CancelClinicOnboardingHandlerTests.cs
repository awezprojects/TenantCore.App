using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Onboarding.Commands;
using TenantCore.Application.Features.Onboarding.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Commands;

public class CancelClinicOnboardingHandlerTests
{
    private readonly Mock<IClinicOnboardingRequestRepository> _requestRepository = new();

    private CancelClinicOnboardingHandler CreateHandler() => new(_requestRepository.Object);

    private static ClinicOnboardingRequest CreateSubmittedRequest(Guid userId) => ClinicOnboardingRequest.Submit(
        userId, "Requester", "requester@example.test", "9876543210", "Clinic", "CODE1", "addr", "city", "state", "411001",
        null, null, null, "Doc", "MR1", "MCI", 1, null, null);

    [Fact]
    public async Task Handle_OwnerCancelsSubmittedRequest_MovesToCancelled()
    {
        var userId = Guid.NewGuid();
        var request = CreateSubmittedRequest(userId);
        _requestRepository.Setup(r => r.GetByIdForUserAsync(request.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _requestRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(new CancelClinicOnboardingCommand(request.Id, userId), CancellationToken.None);

        request.Status.Should().Be(ClinicOnboardingStatus.Cancelled);
        _requestRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonSubmittedRequest_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var request = CreateSubmittedRequest(userId);
        request.ApproveWithTrial(null, null, Guid.NewGuid(), "admin@example.test");
        _requestRepository.Setup(r => r.GetByIdForUserAsync(request.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CancelClinicOnboardingCommand(request.Id, userId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_AnotherUsersRequest_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        _requestRepository.Setup(r => r.GetByIdForUserAsync(requestId, userId, It.IsAny<CancellationToken>())).ReturnsAsync((ClinicOnboardingRequest?)null);

        var handler = CreateHandler();
        var action = () => handler.Handle(new CancelClinicOnboardingCommand(requestId, userId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
