using FluentAssertions;
using Moq;
using TenantCore.Application.Features.DoctorFeeConfigs.Handlers;
using TenantCore.Application.Features.DoctorFeeConfigs.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.DoctorFeeConfigs.Handlers;

public class GetDoctorFeeConfigByIdHandlerTests
{
    private readonly Mock<IDoctorFeeConfigRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        var appId = Guid.NewGuid();
        var config = DoctorFeeConfig.Create(appId, Guid.NewGuid(), 500);
        _repository.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var handler = new GetDoctorFeeConfigByIdHandler(_repository.Object);
        var result = await handler.Handle(new GetDoctorFeeConfigByIdQuery(config.Id, appId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(config.Id);
        result.VisitFee.Should().Be(500);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorFeeConfig?)null);

        var handler = new GetDoctorFeeConfigByIdHandler(_repository.Object);
        var action = () => handler.Handle(new GetDoctorFeeConfigByIdQuery(Guid.NewGuid(), appId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenBelongsToDifferentTenant_ThrowsNotFoundException()
    {
        var config = DoctorFeeConfig.Create(Guid.NewGuid(), Guid.NewGuid(), 500);
        _repository.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var handler = new GetDoctorFeeConfigByIdHandler(_repository.Object);
        var action = () => handler.Handle(new GetDoctorFeeConfigByIdQuery(config.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
