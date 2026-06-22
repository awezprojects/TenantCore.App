using FluentAssertions;
using Moq;
using TenantCore.Application.Features.AmountHandovers.Commands;
using TenantCore.Application.Features.AmountHandovers.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.AmountHandovers.Handlers;

public class AcceptAmountHandoverHandlerTests
{
    private readonly Mock<IAmountHandoverRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenPending_AcceptsHandover()
    {
        var appId = Guid.NewGuid();
        var handover = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1000, null);

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handover);
        _repository.Setup(r => r.Update(handover));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AcceptAmountHandoverHandler(_repository.Object);
        var result = await handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        result.Status.Should().Be(HandoverStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AmountHandover?)null);

        var handler = new AcceptAmountHandoverHandler(_repository.Object);
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyAccepted_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var handover = AmountHandover.Create(appId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Doctor", 1000, null);
        handover.Accept();

        _repository.Setup(r => r.GetByIdAsync(handover.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handover);

        var handler = new AcceptAmountHandoverHandler(_repository.Object);
        var action = () => handler.Handle(new AcceptAmountHandoverCommand(handover.Id, appId), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
