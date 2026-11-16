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
    private readonly Mock<IIpdBedTransferHistoryRepository> _historyRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    public DeleteBedHandlerTests()
    {
        _historyRepository.Setup(h => h.HasAnyForBedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accessValidator.Setup(v => v.HasRole(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).Returns(true);
    }

    private DeleteBedHandler CreateHandler() => new(_repository.Object, _historyRepository.Object, _accessValidator.Object);

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

        await CreateHandler().Handle(command, CancellationToken.None);

        _repository.Verify(r => r.Delete(bed), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBedCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bed?)null);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

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

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

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

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("Cannot delete a bed that is currently occupied.");
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallerLacksAdminRoleInBedsClinic_ThrowsNotFoundException()
    {
        // SECURITY: CanAccess alone only proves clinic linkage, not a Clinic Admin role in THIS
        // clinic specifically — a Receptionist here who is also Clinic Admin somewhere else must
        // still be refused.
        var applicationId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        var command = new DeleteBedCommand(bed.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _accessValidator.Setup(v => v.HasRole(applicationId, It.IsAny<IEnumerable<string>>())).Returns(false);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedHasAdmissionHistory_ThrowsInvalidOperationException()
    {
        // DEF-IPD-17: deleting a bed with admission history used to fall through to a raw
        // SqlException (DeleteBehavior.Restrict on IpdBedTransferHistory.BedId) and surface as an
        // unhandled 500 — this must now be a clean, explicit 409 instead.
        var applicationId = Guid.NewGuid();
        var bed = Bed.Create(applicationId, Guid.NewGuid(), Guid.NewGuid(), "B1");
        var command = new DeleteBedCommand(bed.Id, applicationId);

        _repository.Setup(r => r.GetByIdAsync(bed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bed);
        _accessValidator.Setup(v => v.CanAccess(applicationId)).Returns(true);
        _historyRepository.Setup(h => h.HasAnyForBedAsync(bed.Id, applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> action = () => CreateHandler().Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.Delete(It.IsAny<Bed>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
