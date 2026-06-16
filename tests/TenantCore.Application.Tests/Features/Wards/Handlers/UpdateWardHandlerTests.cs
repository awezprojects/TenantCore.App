using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class UpdateWardHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenWardFoundAndNameUnique_UpdatesAndReturnsDto()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new UpdateWardCommand(ward.Id, ward.ApplicationId, "Updated Ward", "Updated description");

        _repository.Setup(r => r.GetByIdWithRoomsAsync(command.Id, command.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _repository.Setup(r => r.ExistsByNameAsync(command.ApplicationId, command.Name, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateWardHandler(_repository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        ward.Name.Should().Be(command.Name);
        ward.Description.Should().Be(command.Description);
        result.Id.Should().Be(ward.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        _repository.Verify(r => r.Update(ward), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWardNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateWardCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated Ward", "Updated description");

        _repository.Setup(r => r.GetByIdWithRoomsAsync(command.Id, command.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ward?)null);

        var handler = new UpdateWardHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyTakenByOther_ThrowsInvalidOperationException()
    {
        var ward = Ward.Create(Guid.NewGuid(), "General Ward", "General patients");
        var command = new UpdateWardCommand(ward.Id, ward.ApplicationId, "Updated Ward", "Updated description");

        _repository.Setup(r => r.GetByIdWithRoomsAsync(command.Id, command.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward);
        _repository.Setup(r => r.ExistsByNameAsync(command.ApplicationId, command.Name, command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateWardHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(UserMessages.WardNameTaken);
        _repository.Verify(r => r.Update(It.IsAny<Ward>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
