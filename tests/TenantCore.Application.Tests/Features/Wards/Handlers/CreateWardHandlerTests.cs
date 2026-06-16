using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Wards.Commands;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class CreateWardHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenNameIsUnique_CreatesWardAndReturnsDto()
    {
        var command = new CreateWardCommand(Guid.NewGuid(), "General Ward", "General patients");
        Ward? capturedWard = null;

        _repository.Setup(r => r.ExistsByNameAsync(command.ApplicationId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.AddAsync(It.IsAny<Ward>(), It.IsAny<CancellationToken>()))
            .Callback<Ward, CancellationToken>((ward, _) => capturedWard = ward)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateWardHandler(_repository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        capturedWard.Should().NotBeNull();
        result.Id.Should().Be(capturedWard!.Id);
        result.ApplicationId.Should().Be(command.ApplicationId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.IsActive.Should().BeTrue();
        _repository.Verify(r => r.AddAsync(It.IsAny<Ward>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsInvalidOperationException()
    {
        var command = new CreateWardCommand(Guid.NewGuid(), "General Ward", "General patients");

        _repository.Setup(r => r.ExistsByNameAsync(command.ApplicationId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateWardHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(UserMessages.WardNameTaken);
        _repository.Verify(r => r.AddAsync(It.IsAny<Ward>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveFails_PropagatesException()
    {
        var command = new CreateWardCommand(Guid.NewGuid(), "General Ward", "General patients");

        _repository.Setup(r => r.ExistsByNameAsync(command.ApplicationId, command.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.AddAsync(It.IsAny<Ward>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var handler = new CreateWardHandler(_repository.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("save failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<Ward>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
