using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Handlers;

public class DeleteDoctorFeeConfigHandlerTests
{
    private readonly Mock<IDoctorFeeConfigRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_DeletesAndReturnsUnit()
    {
        var appId = Guid.NewGuid();
        var entity = DoctorFeeConfig.Create(appId, Guid.NewGuid(), 500);

        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repository.Setup(r => r.Delete(entity));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteDoctorFeeConfigCommand(entity.Id, appId), CancellationToken.None);

        await action.Should().NotThrowAsync();
        _repository.Verify(r => r.Delete(entity), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorFeeConfig?)null);

        var handler = new DeleteDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteDoctorFeeConfigCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var entity = DoctorFeeConfig.Create(Guid.NewGuid(), Guid.NewGuid(), 500);
        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(new DeleteDoctorFeeConfigCommand(entity.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
