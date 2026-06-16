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

public class UpdatePatientHandlerTests
{
    private readonly Mock<IPatientRepository> _repository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenPatientExistsAndAccessGranted_UpdatesAndReturnsDto()
    {
        var patient = CreatePatient();
        var command = new UpdatePatientCommand(
            patient.Id,
            patient.ApplicationId,
            "Updated",
            "Patient",
            new DateOnly(1991, 8, 14),
            Gender.Other,
            "+4412345678",
            "updated@example.com",
            "999988887777",
            "https://cdn.example.com/updated.jpg",
            "Updated Address",
            "A+",
            "Emergency Contact",
            "+447777777777",
            "Dust",
            "Migraines",
            ShowFullAadhaar: true);

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(true);

        var handler = new UpdatePatientHandler(_repository.Object, _accessValidator.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        patient.FirstName.Should().Be(command.FirstName);
        patient.LastName.Should().Be(command.LastName);
        patient.DateOfBirth.Should().Be(command.DateOfBirth);
        patient.Gender.Should().Be(command.Gender);
        patient.PhoneNumber.Should().Be(command.PhoneNumber);
        patient.Email.Should().Be(command.Email);
        patient.AadhaarNumber.Should().Be(command.AadhaarNumber);
        patient.PhotoUrl.Should().Be(command.PhotoUrl);
        patient.Address.Should().Be(command.Address);
        patient.BloodGroup.Should().Be(command.BloodGroup);
        patient.EmergencyContactName.Should().Be(command.EmergencyContactName);
        patient.EmergencyContactPhone.Should().Be(command.EmergencyContactPhone);
        patient.KnownAllergies.Should().Be(command.KnownAllergies);
        patient.MedicalHistory.Should().Be(command.MedicalHistory);
        result.AadhaarNumber.Should().Be(command.AadhaarNumber);
        _repository.Verify(r => r.Update(patient), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPatientNotFound_ThrowsNotFoundException()
    {
        var command = new UpdatePatientCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Updated",
            "Patient",
            new DateOnly(1991, 8, 14),
            Gender.Other,
            "+4412345678",
            "updated@example.com",
            "999988887777",
            null,
            "Updated Address");

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var handler = new UpdatePatientHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsUnauthorizedAccessException()
    {
        var patient = CreatePatient();
        var command = new UpdatePatientCommand(
            patient.Id,
            patient.ApplicationId,
            "Updated",
            "Patient",
            new DateOnly(1991, 8, 14),
            Gender.Other,
            "+4412345678",
            "updated@example.com",
            "999988887777",
            null,
            "Updated Address");

        _repository.Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(false);

        var handler = new UpdatePatientHandler(_repository.Object, _accessValidator.Object);

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
