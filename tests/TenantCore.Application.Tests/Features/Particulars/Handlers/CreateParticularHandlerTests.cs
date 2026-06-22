using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Particulars.Commands;
using TenantCore.Application.Features.Particulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Particulars.Handlers;

public class CreateParticularHandlerTests
{
    private readonly Mock<IParticularRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenValid_CreatesParticularAndReturnsGuid()
    {
        var appId = Guid.NewGuid();
        var command = new CreateParticularCommand(
            new CreateParticularRequest { Name = "Dressing", DefaultAmount = 150 }, appId);

        _repository.Setup(r => r.AddAsync(It.IsAny<Particular>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateParticularHandler(_repository.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.IsAny<Particular>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
