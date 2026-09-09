using FluentAssertions;
using Moq;
using TenantCore.Application.Features.VitalPresets.Handlers;
using TenantCore.Application.Features.VitalPresets.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.VitalPresets.Queries;

public class GetVitalPresetsHandlerTests
{
    private readonly Mock<IVitalPresetLookupItemRepository> _repository = new();

    [Fact]
    public async Task Handle_ItemsExist_ReturnsMappedDtos()
    {
        var appId = Guid.NewGuid();
        var items = new List<VitalPresetLookupItem>
        {
            VitalPresetLookupItem.CreateGlobal(VitalFieldType.Bp, "120/80"),
            VitalPresetLookupItem.CreateForClinic(appId, VitalFieldType.Pulse, "75")
        };
        _repository.Setup(r => r.GetForApplicationAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var handler = new GetVitalPresetsHandler(_repository.Object);
        var result = (await handler.Handle(new GetVitalPresetsQuery(appId), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.IsGlobal && d.Value == "120/80");
        result.Should().Contain(d => !d.IsGlobal && d.Value == "75");
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyList()
    {
        var appId = Guid.NewGuid();
        _repository.Setup(r => r.GetForApplicationAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = new GetVitalPresetsHandler(_repository.Object);
        var result = await handler.Handle(new GetVitalPresetsQuery(appId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
