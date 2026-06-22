using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Particulars.Handlers;
using TenantCore.Application.Features.Particulars.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.Particulars.Handlers;

public class GetParticularByIdHandlerTests
{
    private readonly Mock<IParticularRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        var appId = Guid.NewGuid();
        var particular = Particular.Create(appId, "Dressing", 150);
        _repository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(particular);

        var handler = new GetParticularByIdHandler(_repository.Object);
        var result = await handler.Handle(new GetParticularByIdQuery(particular.Id, appId), CancellationToken.None);

        result.Name.Should().Be("Dressing");
        result.DefaultAmount.Should().Be(150);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Particular?)null);

        var handler = new GetParticularByIdHandler(_repository.Object);
        var action = () => handler.Handle(new GetParticularByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var particular = Particular.Create(Guid.NewGuid(), "Dressing", 150);
        _repository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(particular);

        var handler = new GetParticularByIdHandler(_repository.Object);
        var action = () => handler.Handle(new GetParticularByIdQuery(particular.Id, Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
