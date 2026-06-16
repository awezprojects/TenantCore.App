using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Handlers;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Handlers;

public class DeletePatientHandlerTests
{
    private readonly Mock<IPatientRepository> _repository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenPatientExistsAndAccessGranted_DeactivatesPatient()
    {
        var patient = CreatePatient();
        var command = new DeletePatientCommand(patient.Id, patient.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(true);

        var handler = new DeletePatientHandler(_repository.Object, _accessValidator.Object);

        await handler.Handle(command, CancellationToken.None);

        patient.IsActive.Should().BeFalse();
        _repository.Verify(r => r.Update(patient), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPatientNotFound_ThrowsNotFoundException()
    {
        var command = new DeletePatientCommand(Guid.NewGuid(), Guid.NewGuid());

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var handler = new DeletePatientHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsUnauthorizedAccessException()
    {
        var patient = CreatePatient();
        var command = new DeletePatientCommand(patient.Id, patient.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(false);

        var handler = new DeletePatientHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Access denied.");
        _repository.Verify(r => r.Update(It.IsAny<Patient>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Patient CreatePatient()
        => Patient.Create(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            new DateOnly(1995, 4, 20),
            Gender.Female,
            "+1234567890",
            "jane@example.com",
            "123456789012",
            "https://cdn.example.com/patient.jpg",
            "123 Main St",
            "O+",
            "John Doe",
            "+1987654321",
            "Peanuts",
            "Asthma");
}
