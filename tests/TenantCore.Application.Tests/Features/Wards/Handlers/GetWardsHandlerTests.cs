using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Wards.Handlers;
using TenantCore.Application.Features.Wards.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Wards.Handlers;

public class GetWardsHandlerTests
{
    private readonly Mock<IWardRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsAllWardsForApplication()
    {
        var applicationId = Guid.NewGuid();
        var wards = new[]
        {
            Ward.Create(applicationId, "General Ward", "General patients"),
            Ward.Create(applicationId, "ICU", "Critical care")
        };
        var query = new GetWardsQuery(applicationId);

        _repository.Setup(r => r.GetByApplicationAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wards);

        var handler = new GetWardsHandler(_repository.Object);

        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Select(x => x.Name).Should().BeEquivalentTo(new[] { "General Ward", "ICU" });
        result.Should().OnlyContain(x => x.ApplicationId == applicationId);
    }

    [Fact]
    public async Task Handle_WhenNoWardsExist_ReturnsEmptyCollection()
    {
        var query = new GetWardsQuery(Guid.NewGuid());

        _repository.Setup(r => r.GetByApplicationAsync(query.ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Ward>());

        var handler = new GetWardsHandler(_repository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
