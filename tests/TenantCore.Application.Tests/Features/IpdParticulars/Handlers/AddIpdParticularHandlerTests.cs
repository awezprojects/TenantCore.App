using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdParticulars.Commands;
using TenantCore.Application.Features.IpdParticulars.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Tests.Features.IpdParticulars.Handlers;

public class AddIpdParticularHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IParticularRepository> _particularRepository = new();
    private readonly Mock<IIpdParticularRepository> _ipdParticularRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();

    public AddIpdParticularHandlerTests()
    {
        _accessValidator.Setup(v => v.CanAccess(It.IsAny<Guid>())).Returns(true);
        _ipdParticularRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AddIpdParticularHandler CreateHandler() => new(
        _ipdRepository.Object, _particularRepository.Object, _ipdParticularRepository.Object,
        _paymentRepository.Object, _accessValidator.Object);

    private IpdRegistration BuildAdmittedRegistration() =>
        IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 0, null);

    [Fact]
    public async Task Handle_ValidParticular_IncreasesPaymentParticularsTotal()
    {
        var registration = BuildAdmittedRegistration();
        var particular = Particular.Create(_appId, "X-Ray", 300m);
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _particularRepository.Setup(r => r.GetByIdAsync(particular.Id, It.IsAny<CancellationToken>())).ReturnsAsync(particular);
        _paymentRepository.Setup(r => r.GetByRegistrationAsync(registration.Id, _appId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var result = await CreateHandler().Handle(
            new AddIpdParticularCommand(registration.Id, _appId, particular.Id, 300m, Guid.NewGuid()), CancellationToken.None);

        result.ParticularName.Should().Be("X-Ray");
        payment.ParticularsTotal.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_ParticularFromDifferentTenant_ThrowsNotFoundException()
    {
        var registration = BuildAdmittedRegistration();
        var foreignParticular = Particular.Create(Guid.NewGuid(), "X-Ray", 300m);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        _particularRepository.Setup(r => r.GetByIdAsync(foreignParticular.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreignParticular);

        var act = () => CreateHandler().Handle(
            new AddIpdParticularCommand(registration.Id, _appId, foreignParticular.Id, 300m, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NotAdmitted_ThrowsInvalidOperationException()
    {
        var registration = BuildAdmittedRegistration();
        registration.Discharge(null);
        _ipdRepository.Setup(r => r.GetByIdAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);

        var act = () => CreateHandler().Handle(
            new AddIpdParticularCommand(registration.Id, _appId, Guid.NewGuid(), 300m, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
