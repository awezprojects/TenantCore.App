using FluentAssertions;
using Moq;
using TenantCore.Application.Features.ClinicSettings.Handlers;
using TenantCore.Application.Features.ClinicSettings.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.ClinicSettings.Queries;

public class GetClinicFeatureFlagsHandlerTests
{
    private readonly Mock<IClinicFeatureFlagsRepository> _repository = new();

    [Fact]
    public async Task Handle_ExistingFlags_ReturnsMappedDto()
    {
        var appId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(appId, false);
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        var handler = new GetClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new GetClinicFeatureFlagsQuery(appId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PrepaidOpdEnabled.Should().BeFalse();
        result.ApplicationId.Should().Be(appId);
        _repository.Verify(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoExistingFlags_ReturnsNullWithoutWriting()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetByApplicationAsync(appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClinicFeatureFlags?)null);

        var handler = new GetClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new GetClinicFeatureFlagsQuery(appId), CancellationToken.None);

        result.Should().BeNull();
        _repository.Verify(r => r.AddAsync(It.IsAny<ClinicFeatureFlags>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EntityBelongingToDifferentTenant_QueriesByCorrectApplicationId()
    {
        var commandApplicationId = Guid.NewGuid();
        var flags = ClinicFeatureFlags.Create(commandApplicationId);
        _repository.Setup(r => r.GetByApplicationAsync(commandApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        var handler = new GetClinicFeatureFlagsHandler(_repository.Object);
        var result = await handler.Handle(new GetClinicFeatureFlagsQuery(commandApplicationId), CancellationToken.None);

        result!.ApplicationId.Should().Be(commandApplicationId);
        _repository.Verify(r => r.GetByApplicationAsync(commandApplicationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
