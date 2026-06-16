using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Application.Features.Beds.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Beds.Handlers;

public class DeleteBedHandlerTests
{
    private readonly Mock<IBedRepository> _repository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenBedFoundAndNotOccupied_DeletesBed()
    {
        var applicationId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        var command = new DeleteBedCommand(bed.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bed);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteBedHandler(_repository.Object, _accessValidator.Object);

        await handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(bed), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBedCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bed?)null);

        var handler = new DeleteBedHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsNotFoundException()
    {
        var applicationId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        var command = new DeleteBedCommand(bed.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bed);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(false);

        var handler = new DeleteBedHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedIsOccupied_ThrowsDomainValidationException()
    {
        var applicationId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        bed.MarkOccupied();
        var command = new DeleteBedCommand(bed.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bed);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);

        var handler = new DeleteBedHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("Cannot delete a bed that is currently occupied.");
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
