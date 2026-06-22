using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Application.Features.Particulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Particulars.Handlers;

public class UpdateParticularHandlerTests
{
    private readonly Mock<IParticularRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenFound_UpdatesAndReturnsDto()
    {
        var appId = Guid.NewGuid();
        var entity = Particular.Create(appId, "Old Name", 100);

        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repository.Setup(r => r.Update(entity));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateParticularCommand(
            entity.Id, new UpdateParticularRequest { Name = "New Name", DefaultAmount = 200, IsActive = true }, appId);

        var handler = new UpdateParticularHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("New Name");
        result.DefaultAmount.Should().Be(200);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Particular?)null);

        var handler = new UpdateParticularHandler(_repository.Object);
        var action = () => handler.Handle(
            new UpdateParticularCommand(Guid.NewGuid(), new UpdateParticularRequest { Name = "X", DefaultAmount = 10 }, Guid.NewGuid()),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCrossTenant_ThrowsNotFoundException()
    {
        var entity = Particular.Create(Guid.NewGuid(), "Test", 100);
        _repository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new UpdateParticularHandler(_repository.Object);
        var action = () => handler.Handle(
            new UpdateParticularCommand(entity.Id, new UpdateParticularRequest { Name = "X", DefaultAmount = 10 }, Guid.NewGuid()),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
