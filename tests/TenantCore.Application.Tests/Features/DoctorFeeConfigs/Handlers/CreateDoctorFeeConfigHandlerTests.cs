using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DoctorFeeConfigs.Commands;
using TenantCore.Application.Features.DoctorFeeConfigs.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Handlers;

public class CreateDoctorFeeConfigHandlerTests
{
    private readonly Mock<IDoctorFeeConfigRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenValid_CreatesFeeConfigAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var request = new CreateDoctorFeeConfigRequest { DoctorProfileId = doctorProfileId, VisitFee = 500 };
        var command = new CreateDoctorFeeConfigCommand(request, appId);

        _repository.Setup(r => r.GetByDoctorProfileIdAsync(doctorProfileId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorFeeConfig?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<DoctorFeeConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateDoctorFeeConfigHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.IsAny<DoctorFeeConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFeeConfigAlreadyExists_ThrowsInvalidOperationException()
    {
        var appId = Guid.NewGuid();
        var doctorProfileId = Guid.NewGuid();
        var existing = DoctorFeeConfig.Create(appId, doctorProfileId, 300);
        var request = new CreateDoctorFeeConfigRequest { DoctorProfileId = doctorProfileId, VisitFee = 500 };
        var command = new CreateDoctorFeeConfigCommand(request, appId);

        _repository.Setup(r => r.GetByDoctorProfileIdAsync(doctorProfileId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new CreateDoctorFeeConfigHandler(_repository.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<DoctorFeeConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
