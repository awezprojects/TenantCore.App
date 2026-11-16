using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Application.Features.IpdParticulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdParticulars.Handlers;

public class RemoveIpdParticularHandlerTests
{
    private readonly Mock<IIpdParticularRepository> _ipdParticularRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public RemoveIpdParticularHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _ipdParticularRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private RemoveIpdParticularHandler CreateHandler() => new(_ipdParticularRepository.Object, _paymentRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_ExistingParticular_DecreasesPaymentParticularsTotalAndDeletes()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var particular = IpdParticular.Create(_appId, ipdRegistrationId, Guid.NewGuid(), "X-Ray", 300m, Guid.NewGuid());
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m);
        payment.IncreaseParticularsTotal(300m);
        _ipdParticularRepository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>())).ReturnsAsync(particular);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        await CreateHandler().Handle(new RemoveIpdParticularCommand(particular.Id, _appId), CancellationToken.None);

        payment.ParticularsTotal.Should().Be(0);
        _ipdParticularRepository.Verify(r => r.Delete(particular), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _ipdParticularRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((IpdParticular?)null);

        var act = () => CreateHandler().Handle(new RemoveIpdParticularCommand(Guid.NewGuid(), _appId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentTenant_ThrowsNotFoundException()
    {
        var particular = IpdParticular.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X-Ray", 300m, Guid.NewGuid());
        _ipdParticularRepository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>())).ReturnsAsync(particular);

        var act = () => CreateHandler().Handle(new RemoveIpdParticularCommand(particular.Id, _appId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BillFinalized_ThrowsInvalidOperationException()
    {
        var ipdRegistrationId = Guid.NewGuid();
        var particular = IpdParticular.Create(_appId, ipdRegistrationId, Guid.NewGuid(), "X-Ray", 300m, Guid.NewGuid());
        var payment = IpdPayment.Create(_appId, ipdRegistrationId, 2000m);
        payment.FinalizeAtDischarge(0, DateTime.UtcNow);
        _ipdParticularRepository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>())).ReturnsAsync(particular);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(ipdRegistrationId, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var act = () => CreateHandler().Handle(new RemoveIpdParticularCommand(particular.Id, _appId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
