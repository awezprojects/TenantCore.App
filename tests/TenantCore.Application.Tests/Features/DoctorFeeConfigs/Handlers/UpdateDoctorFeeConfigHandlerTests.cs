using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Handlers;

public class UpdateDoctorFeeConfigHandlerTests
{
    private readonly Mock<IDoctorFeeConfigRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_UpdatesAndReturnsDto()
    {
        var appId = Guid.NewGuid();
        var entity = DoctorFeeConfig.Create(appId, Guid.NewGuid(), 500);

        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repository.Setup(r => r.Update(entity));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateDoctorFeeConfigCommand(
            entity.Id, new UpdateDoctorFeeConfigRequest { VisitFee = 750, IsActive = true }, appId);

        var handler = new UpdateDoctorFeeConfigHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.VisitFee.Should().Be(750);
        _repository.Verify(r => r.Update(entity), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorFeeConfig?)null);

        var command = new UpdateDoctorFeeConfigCommand(
            Guid.NewGuid(), new UpdateDoctorFeeConfigRequest { VisitFee = 750 }, Guid.NewGuid());

        var handler = new UpdateDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var entity = DoctorFeeConfig.Create(Guid.NewGuid(), Guid.NewGuid(), 500);
        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var command = new UpdateDoctorFeeConfigCommand(
            entity.Id, new UpdateDoctorFeeConfigRequest { VisitFee = 750 }, Guid.NewGuid());

        var handler = new UpdateDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
