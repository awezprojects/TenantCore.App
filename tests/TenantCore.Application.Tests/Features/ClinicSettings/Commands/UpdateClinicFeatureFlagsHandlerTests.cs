using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ClinicSettings.Commands;
using TenantCore.Application.Features.ClinicSettings.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Commands;

public class UpdateClinicFeatureFlagsHandlerTests
{
    private readonly Mock<IClinicFeatureFlagsRepository> _repository = new();

    [Fact]
    public async Task Handle_ExistingFlags_UpdatesPrepaidOpdEnabled()
    {
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, true);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(appId, false), CancellationToken.None);

        result.PrepaidOpdEnabled.Should().BeFalse();
        _repository.Verify(r => r.Update(flags), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoExistingFlags_CreatesThenReturnsUpdatedValue()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new UpdateClinicFeatureFlagsCommand(appId, false), CancellationToken.None);

        result.PrepaidOpdEnabled.Should().BeFalse();
        result.ApplicationId.Should().Be(appId);
        _repository.Verify(r => r.AddAsync(It.Is<ClinicFeatureFlags>(f => f.ApplicationId == appId && !f.PrepaidOpdEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }
}
