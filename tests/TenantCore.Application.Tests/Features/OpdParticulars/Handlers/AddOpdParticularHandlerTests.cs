using FluentAssertions;
using Moq;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Application.Features.OpdParticulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.OpdParticulars.Handlers;

public class AddOpdParticularHandlerTests
{
    private readonly Mock<IOpdParticularRepository> _opdParticularRepo = new();
    private readonly Mock<IOpdPaymentRepository> _paymentRepo = new();
    private readonly Mock<IParticularRepository> _particularRepo = new();

    [Fact]
    public async Task Handle_WhenParticularExists_AddsItemAndReturnsDto()
    {
        var appId = Guid.NewGuid();
        var opdId = Guid.NewGuid();
        var particular = Particular.Create(appId, "X-Ray", 300);

        _particularRepo.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(particular);
        _opdParticularRepo.Setup(r => r.AddAsync(It.IsAny<OpdParticular>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _opdParticularRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _opdParticularRepo.Setup(r => r.GetTotalByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(300m);
        _paymentRepo.Setup(r => r.GetByOpdRegistrationIdAsync(opdId, appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpdPayment?)null);

        var command = new AddOpdParticularCommand(
            new AddOpdParticularRequest { OpdRegistrationId = opdId, ParticularId = particular.Id, Amount = 300 },
            appId);

        var handler = new AddOpdParticularHandler(_opdParticularRepo.Object, _paymentRepo.Object, _particularRepo.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ParticularId.Should().Be(particular.Id);
        _opdParticularRepo.Verify(r => r.AddAsync(It.IsAny<OpdParticular>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenParticularNotFound_ThrowsNotFoundException()
    {
        _particularRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Particular?)null);

        var command = new AddOpdParticularCommand(
            new AddOpdParticularRequest { OpdRegistrationId = Guid.NewGuid(), ParticularId = Guid.NewGuid(), Amount = 300 },
            Guid.NewGuid());

        var handler = new AddOpdParticularHandler(_opdParticularRepo.Object, _paymentRepo.Object, _particularRepo.Object);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
