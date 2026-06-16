using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Handlers;
using TenantCore.Application.Tests.Common.Logging;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Handlers;

public class RegisterPatientHandlerTests
{
    private readonly Mock<IPatientRepository> _repository = new();
    private readonly Mock<ILogger<RegisterPatientHandler>> _logger = new();

    [Fact]
    public async Task Handle_WhenValidCommand_CreatesPatientAndReturnsDto()
    {
        var command = CreateCommand(showFullAadhaar: true);
        Patient? capturedPatient = null;

        _repository.Setup(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .Callback<Patient, CancellationToken>((patient, _) => capturedPatient = patient)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RegisterPatientHandler(_repository.Object, _logger.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        capturedPatient.Should().NotBeNull();
        result.Id.Should().Be(capturedPatient!.Id);
        result.ApplicationId.Should().Be(command.ApplicationId);
        result.FirstName.Should().Be(command.FirstName);
        result.LastName.Should().Be(command.LastName);
        result.DateOfBirth.Should().Be(command.DateOfBirth);
        result.Gender.Should().Be(command.Gender);
        result.PhoneNumber.Should().Be(command.PhoneNumber);
        result.Email.Should().Be(command.Email);
        result.AadhaarNumber.Should().Be(command.AadhaarNumber);
        result.PhotoUrl.Should().Be(command.PhotoUrl);
        result.Address.Should().Be(command.Address);
        result.BloodGroup.Should().Be(command.BloodGroup);
        result.EmergencyContactName.Should().Be(command.EmergencyContactName);
        result.EmergencyContactPhone.Should().Be(command.EmergencyContactPhone);
        result.KnownAllergies.Should().Be(command.KnownAllergies);
        result.MedicalHistory.Should().Be(command.MedicalHistory);
        result.IsActive.Should().BeTrue();
        _repository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.VerifyLog(LogLevel.Information, "Registering patient", Times.Once());
    }

    [Fact]
    public async Task Handle_WhenSaveFails_PropagatesException()
    {
        var command = CreateCommand();

        _repository.Setup(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var handler = new RegisterPatientHandler(_repository.Object, _logger.Object);

        Func<Task> action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("save failed");
        _repository.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RegisterPatientCommand CreateCommand(
        Guid? applicationId = null,
        string firstName = "Jane",
        string lastName = "Doe",
        bool showFullAadhaar = false)
        => new(
            applicationId ?? Guid.NewGuid(),
            firstName,
            lastName,
            new DateOnly(1995, 4, 20),
            Gender.Female,
            "+1234567890",
            "jane@example.com",
            "123456789012",
            "https://cdn.example.com/patients/jane.jpg",
            "123 Main St",
            "O+",
            "John Doe",
            "+1987654321",
            "Peanuts",
            "Asthma",
            showFullAadhaar);
}
