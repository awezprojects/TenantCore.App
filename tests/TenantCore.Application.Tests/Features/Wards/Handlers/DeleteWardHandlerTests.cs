using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class DeleteWardHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenWardFoundAndAccessGranted_DeletesWard()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId))
            .Returns(true);

        var handler = new DeleteWardHandler(_repository.Object, _accessValidator.Object);

        await handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(ward), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWardNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteWardCommand(Guid.NewGuid(), Guid.NewGuid());

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ward?)null);

        var handler = new DeleteWardHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new DeleteWardCommand(ward.Id, ward.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _accessValidator.Setup(v => v.CanAccess(ward.ApplicationId))
            .Returns(false);

        var handler = new DeleteWardHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
